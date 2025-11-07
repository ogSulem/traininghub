using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using TrainingHub.CourseService.Application.Assignments;

namespace TrainingHub.CourseService.Infrastructure.Messaging;

public class RabbitMqAssignmentEventPublisher : IAssignmentEventPublisher, IDisposable
{
    private readonly RabbitMqOptions _options;
    private readonly ILogger<RabbitMqAssignmentEventPublisher> _logger;
    private IConnection? _connection;
    private IModel? _channel;

    public RabbitMqAssignmentEventPublisher(IOptions<RabbitMqOptions> options, ILogger<RabbitMqAssignmentEventPublisher> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task PublishAssignmentScheduledAsync(AssignmentScheduledEvent @event, CancellationToken cancellationToken)
    {
        var channel = EnsureChannel();

        var payload = JsonSerializer.Serialize(@event);
        var body = Encoding.UTF8.GetBytes(payload);

        var props = channel.CreateBasicProperties();
        props.Persistent = true;

        var routingKey = $"assignments.scheduled.{@event.CourseId}";

        channel.BasicPublish(
            exchange: _options.Exchange,
            routingKey: routingKey,
            basicProperties: props,
            body: body);

        _logger.LogInformation("Published AssignmentScheduled event {@Event}", @event);
        await Task.CompletedTask;
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

        return factory.CreateConnection("traininghub-course-service");
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }
}
