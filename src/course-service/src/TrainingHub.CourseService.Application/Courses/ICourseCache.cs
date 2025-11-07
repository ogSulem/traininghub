using TrainingHub.CourseService.Domain.Entities;

namespace TrainingHub.CourseService.Application.Courses;

public interface ICourseCache
{
    Task<IReadOnlyCollection<Course>> GetPopularAsync(int take, CancellationToken cancellationToken);
    Task SetPopularAsync(int take, IReadOnlyCollection<Course> courses, TimeSpan ttl, CancellationToken cancellationToken);
}
