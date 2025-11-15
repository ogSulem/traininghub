using Microsoft.Extensions.Logging;
using Quartz;
using System.Net.Http.Json;
using TrainingHub.CourseService.Application.Notifications;

namespace TrainingHub.CourseService.Worker.Quartz.Jobs;

public class DailyReminderJob : IJob
{
    private readonly ILogger<DailyReminderJob> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly INotificationDispatcher _notificationDispatcher;

    public DailyReminderJob(ILogger<DailyReminderJob> logger, IHttpClientFactory httpClientFactory, INotificationDispatcher notificationDispatcher)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _notificationDispatcher = notificationDispatcher;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("Running DailyReminderJob at {Time}", DateTimeOffset.UtcNow);

        var client = _httpClientFactory.CreateClient("CourseService");
        var response = await client.GetAsync($"/courses?popular=true&take=5", context.CancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Failed to fetch courses for reminders: {StatusCode}", response.StatusCode);
            return;
        }

        var courses = await response.Content.ReadFromJsonAsync<List<CourseDto>>(cancellationToken: context.CancellationToken) ?? new();
        foreach (var course in courses)
        {
            _logger.LogInformation("Reminder: course {CourseId} {Title} is active", course.Id, course.Title);

            var title = $"Ежедневное напоминание по курсу \"{course.Title}\"";
            var dueDate = DateTime.UtcNow;

            await _notificationDispatcher.DispatchAssignmentScheduledAsync(
                course.Id,
                Guid.Empty,
                title,
                dueDate,
                context.CancellationToken);
        }
    }

    private record CourseDto(Guid Id, string Title);
}
