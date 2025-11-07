namespace TrainingHub.CourseService.Application.Assignments;

public record AssignmentScheduledEvent(
    Guid AssignmentId,
    Guid CourseId,
    string Title,
    DateTime DueDate,
    DateTime CreatedAtUtc);
