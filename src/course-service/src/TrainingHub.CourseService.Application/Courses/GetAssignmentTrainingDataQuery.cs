using MediatR;

namespace TrainingHub.CourseService.Application.Courses;

public record GetAssignmentTrainingDataQuery : IRequest<IReadOnlyCollection<AssignmentTrainingData>>;

public class GetAssignmentTrainingDataQueryHandler : IRequestHandler<GetAssignmentTrainingDataQuery, IReadOnlyCollection<AssignmentTrainingData>>
{
    private readonly ICourseRepository _repository;

    public GetAssignmentTrainingDataQueryHandler(ICourseRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyCollection<AssignmentTrainingData>> Handle(GetAssignmentTrainingDataQuery request, CancellationToken cancellationToken)
    {
        return await _repository.GetAssignmentTrainingDataAsync(cancellationToken);
    }
}
