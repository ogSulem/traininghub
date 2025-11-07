using MediatR;
using TrainingHub.CourseService.Domain.Entities;

namespace TrainingHub.CourseService.Application.Courses;

public record GetPopularCoursesQuery(int Take = 5) : IRequest<IReadOnlyCollection<Course>>;

public class GetPopularCoursesQueryHandler : IRequestHandler<GetPopularCoursesQuery, IReadOnlyCollection<Course>>
{
    private readonly ICourseRepository _repository;
    private readonly ICourseCache _cache;

    public GetPopularCoursesQueryHandler(ICourseRepository repository, ICourseCache cache)
    {
        _repository = repository;
        _cache = cache;
    }

    public async Task<IReadOnlyCollection<Course>> Handle(GetPopularCoursesQuery request, CancellationToken cancellationToken)
    {
        var take = request.Take <= 0 ? 5 : request.Take;

        var cached = await _cache.GetPopularAsync(take, cancellationToken);
        if (cached.Count > 0)
        {
            return cached;
        }

        var popular = await _repository.GetPopularAsync(take, cancellationToken);
        await _cache.SetPopularAsync(take, popular, TimeSpan.FromMinutes(5), cancellationToken);
        return popular;
    }
}
