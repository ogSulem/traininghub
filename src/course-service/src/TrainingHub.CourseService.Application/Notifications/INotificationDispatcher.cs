namespace TrainingHub.CourseService.Application.Notifications;

public interface INotificationDispatcher
{
    Task DispatchAssignmentScheduledAsync(Guid courseId, Guid assignmentId, string title, DateTime dueDate, CancellationToken cancellationToken);
}
