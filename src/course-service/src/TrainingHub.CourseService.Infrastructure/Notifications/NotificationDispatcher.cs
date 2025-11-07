using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TrainingHub.CourseService.Application.Notifications;

namespace TrainingHub.CourseService.Infrastructure.Notifications;

public class NotificationDispatcher : INotificationDispatcher
{
    private readonly HttpClient _httpClient;
    private readonly NotificationGatewayOptions _options;
    private readonly ILogger<NotificationDispatcher> _logger;

    public NotificationDispatcher(HttpClient httpClient, IOptions<NotificationGatewayOptions> options, ILogger<NotificationDispatcher> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task DispatchAssignmentScheduledAsync(Guid courseId, Guid assignmentId, string title, DateTime dueDate, CancellationToken cancellationToken)
    {
        var payload = new AssignmentNotificationDto(courseId, assignmentId, title, dueDate);

        try
        {
            var response = await _httpClient.PostAsJsonAsync(_options.AssignmentsEndpoint, payload, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Notification gateway responded with {StatusCode}", response.StatusCode);
            }
            else
            {
                _logger.LogInformation("Dispatched assignment notification for course {CourseId}", courseId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to dispatch assignment notification");
            throw;
        }
    }

    private record AssignmentNotificationDto(Guid CourseId, Guid AssignmentId, string Title, DateTime DueDate);
}
