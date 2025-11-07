using System.Text;
using System.Text.Json;
using System.Threading;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using TrainingHub.CourseService.Application.Assignments;
using TrainingHub.CourseService.Application.Notifications;

namespace TrainingHub.CourseService.Infrastructure.Messaging;

public class RabbitMqAssignmentEventConsumer : BackgroundService
{
    private readonly RabbitMqOptions _options;
    private readonly ILogger<RabbitMqAssignmentEventConsumer> _logger;
    private readonly INotificationDispatcher _notificationDispatcher;
    private IConnection? _connection;
    private IModel? _channel;

    public RabbitMqAssignmentEventConsumer(
        IOptions<RabbitMqOptions> options,
        ILogger<RabbitMqAssignmentEventConsumer> logger,
        INotificationDispatcher notificationDispatcher)
    {
        _options = options.Value;
        _logger = logger;
        _notificationDispatcher = notificationDispatcher;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var channel = EnsureChannel();

                var queueName = channel.QueueDeclare(queue: "traininghub.notifications",
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null).QueueName;

                channel.QueueBind(queue: queueName,
                    exchange: _options.Exchange,
                    routingKey: "assignments.scheduled.*");

                var consumer = new AsyncEventingBasicConsumer(channel);
                consumer.Received += async (_, args) =>
                {
                    try
                    {
                        var body = Encoding.UTF8.GetString(args.Body.ToArray());
                        var payload = JsonSerializer.Deserialize<AssignmentScheduledEvent>(body);
                        if (payload is not null)
                        {
                            _logger.LogInformation("Received AssignmentScheduled event {@Event}", payload);
                            await _notificationDispatcher.DispatchAssignmentScheduledAsync(
                                payload.CourseId,
                                payload.AssignmentId,
                                payload.Title,
                                payload.DueDate,
                                stoppingToken);
                        }
                        channel.BasicAck(args.DeliveryTag, multiple: false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to process assignment event");
                        channel.BasicNack(args.DeliveryTag, multiple: false, requeue: true);
                    }

                    await Task.CompletedTask;
                };

                channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);

                _logger.LogInformation(
                    "RabbitMQ consumer started (queue={Queue}, exchange={Exchange}, bindingKey={BindingKey})",
                    queueName,
                    _options.Exchange,
                    "assignments.scheduled.*");

                // Keep the hosted service alive while RabbitMQ is consuming.
                // If this method returns, the BackgroundService stops and consumption may be interrupted.
                try
                {
                    await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // host is shutting down
                }

                return;
            }
            catch (BrokerUnreachableException ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogWarning("RabbitMQ broker is unreachable, will retry in 5 seconds. Reason: {Reason}", ex.Message);
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // host is shutting down
                    return;
                }
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError("Unexpected error while starting RabbitMQ consumer, will retry in 5 seconds. Reason: {Reason}", ex.Message);
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // host is shutting down
                    return;
                }
            }
        }
    }

    private IModel EnsureChannel()
    {
        if (_channel is { IsClosed: false })
        {
            return _channel;
        }

        _connection ??= CreateConnection();
        _channel = _connection.CreateModel();
        _channel.ExchangeDeclare(exchange: _options.Exchange, type: ExchangeType.Topic, durable: true, autoDelete: false);
        return _channel;
    }

    private IConnection CreateConnection()
    {
        var factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password,
            DispatchConsumersAsync = true
        };

        return factory.CreateConnection("traininghub-notifications");
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}
