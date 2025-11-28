namespace TrainingHub.ApiGateway.Contracts.Responses;

public record CourseResponse(
    Guid Id,
    string Title,
    string Description,
    DateTime StartsAt,
    DateTime EndsAt
);
