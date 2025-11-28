namespace TrainingHub.ApiGateway.Contracts.Requests;

public class CreateCourseRequest
{
    public required string Title { get; init; }
    public required string Description { get; init; }
    public DateTime StartsAt { get; init; }
    public DateTime EndsAt { get; init; }
}
