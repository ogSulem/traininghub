using MediatR;
using TrainingHub.CourseService.Domain.Entities;

namespace TrainingHub.CourseService.Application.Courses;

public record GetCourseQuery(Guid Id) : IRequest<Course?>;

public class GetCourseQueryHandler : IRequestHandler<GetCourseQuery, Course?>
{
    private readonly ICourseRepository _repository;

    public GetCourseQueryHandler(ICourseRepository repository)
    {
        _repository = repository;
    }

    public async Task<Course?> Handle(GetCourseQuery request, CancellationToken cancellationToken)
    {
        return await _repository.GetByIdAsync(request.Id, cancellationToken);
    }
}
