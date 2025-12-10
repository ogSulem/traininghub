namespace TrainingHub.BlazorClient.Shared;

public record CourseDto(Guid Id, string Title, string Description);

public record CourseDetailsDto(Guid Id, string Title, string Description, IReadOnlyCollection<AssignmentDto> Assignments);

public record AssignmentDto(Guid Id, Guid CourseId, string Title, DateTime DueDate, string Description, int Status);

public record CreateCourseRequest(string Title, string Description, DateTime StartsAt, DateTime EndsAt);

public record ScheduleAssignmentRequest(string Title, DateTime DueDate, string Description);

public record AssignmentNotificationDto(Guid CourseId, Guid AssignmentId, string Title, DateTime DueDate);
