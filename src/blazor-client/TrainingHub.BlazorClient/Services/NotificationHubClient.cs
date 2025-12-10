using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using TrainingHub.BlazorClient.Shared;

namespace TrainingHub.BlazorClient.Services;

public class NotificationHubClient : INotificationHubClient
{
    private readonly NavigationManager _navigationManager;
    private HubConnection? _connection;

    public event Func<AssignmentNotificationDto, Task>? OnAssignmentScheduled;

    public NotificationHubClient(NavigationManager navigationManager)
    {
        _navigationManager = navigationManager;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_connection is not null)
        {
            return;
        }

        var hubUrl = BuildHubUrl(_navigationManager.BaseUri);
        _connection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .WithAutomaticReconnect()
            .Build();

        _connection.On<AssignmentNotificationDto>("AssignmentScheduled", async notification =>
        {
            if (OnAssignmentScheduled is not null)
            {
                await OnAssignmentScheduled.Invoke(notification);
            }
        });

        await _connection.StartAsync(cancellationToken);
    }

    internal static string BuildHubUrl(string baseUri)
    {
        if (baseUri.StartsWith("http://localhost:5173", StringComparison.OrdinalIgnoreCase))
        {
            baseUri = "http://localhost:5000/";
        }

        return baseUri.TrimEnd('/') + "/hub/notifications";
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }
    }
}
