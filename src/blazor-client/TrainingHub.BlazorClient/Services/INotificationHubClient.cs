using TrainingHub.BlazorClient.Shared;

namespace TrainingHub.BlazorClient.Services;

public interface INotificationHubClient : IAsyncDisposable
{
    event Func<AssignmentNotificationDto, Task>? OnAssignmentScheduled;

    Task StartAsync(CancellationToken cancellationToken = default);
}
