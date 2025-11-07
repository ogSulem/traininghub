namespace TrainingHub.CourseService.Application.Courses;

public record AssignmentTrainingData(Guid CourseId, Guid AssignmentId, DateTime DueDate, bool Completed);
