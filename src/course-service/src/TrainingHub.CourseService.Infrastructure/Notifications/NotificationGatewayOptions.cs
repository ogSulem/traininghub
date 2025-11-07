namespace TrainingHub.CourseService.Infrastructure.Notifications;

public class NotificationGatewayOptions
{
    public string BaseAddress { get; set; } = "https://localhost:5003";
    public string AssignmentsEndpoint { get; set; } = "/internal/notifications/assignments";
}
