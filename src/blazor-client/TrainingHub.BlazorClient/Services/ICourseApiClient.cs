using TrainingHub.BlazorClient.Shared;

namespace TrainingHub.BlazorClient.Services;

public interface ICourseApiClient
{
    Task<IReadOnlyList<CourseDto>> GetPopularCoursesAsync(int take = 5, CancellationToken cancellationToken = default);
    Task<CourseDetailsDto?> GetCourseAsync(Guid courseId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AssignmentDto>> GetAssignmentsAsync(Guid courseId, CancellationToken cancellationToken = default);
    Task<Guid?> CreateCourseAsync(CreateCourseRequest request, CancellationToken cancellationToken = default);
    Task<Guid?> ScheduleAssignmentAsync(Guid courseId, ScheduleAssignmentRequest request, CancellationToken cancellationToken = default);
}
