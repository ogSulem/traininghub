using MediatR;
using TrainingHub.CourseService.Application.Courses;
using TrainingHub.CourseService.Domain.Entities;

namespace TrainingHub.CourseService.Application.Assignments;

public record GetAssignmentsQuery(Guid CourseId) : IRequest<IReadOnlyCollection<Assignment>>;

public class GetAssignmentsQueryHandler : IRequestHandler<GetAssignmentsQuery, IReadOnlyCollection<Assignment>>
{
    private readonly ICourseRepository _courseRepository;

    public GetAssignmentsQueryHandler(ICourseRepository courseRepository)
    {
        _courseRepository = courseRepository;
    }

    public async Task<IReadOnlyCollection<Assignment>> Handle(GetAssignmentsQuery request, CancellationToken cancellationToken)
    {
        return await _courseRepository.GetAssignmentsAsync(request.CourseId, cancellationToken);
    }
}
