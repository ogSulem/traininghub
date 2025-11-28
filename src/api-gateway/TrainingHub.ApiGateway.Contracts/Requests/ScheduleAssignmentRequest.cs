namespace TrainingHub.ApiGateway.Contracts.Requests;

public record ScheduleAssignmentRequest(string Title, DateTime DueDate, string Description);
