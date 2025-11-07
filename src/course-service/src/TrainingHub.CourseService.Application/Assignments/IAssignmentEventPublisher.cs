namespace TrainingHub.CourseService.Application.Assignments;

public interface IAssignmentEventPublisher
{
    Task PublishAssignmentScheduledAsync(AssignmentScheduledEvent @event, CancellationToken cancellationToken);
}
