using System.Net.Http.Json;
using TrainingHub.BlazorClient.Shared;

namespace TrainingHub.BlazorClient.Services;

public class CourseApiClient : ICourseApiClient
{
    private readonly HttpClient _httpClient;

    public CourseApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<CourseDto>> GetPopularCoursesAsync(int take = 5, CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<IReadOnlyList<CourseDto>>($"/courses?popular=true&take={take}", cancellationToken)
               ?? Array.Empty<CourseDto>();
    }

    public async Task<CourseDetailsDto?> GetCourseAsync(Guid courseId, CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<CourseDetailsDto>($"/courses/{courseId}", cancellationToken);
    }

    public async Task<IReadOnlyList<AssignmentDto>> GetAssignmentsAsync(Guid courseId, CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<IReadOnlyList<AssignmentDto>>($"/courses/{courseId}/assignments", cancellationToken)
               ?? Array.Empty<AssignmentDto>();
    }

    public async Task<Guid?> CreateCourseAsync(CreateCourseRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("/courses", request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var payload = await response.Content.ReadFromJsonAsync<CreatedResponse>(cancellationToken: cancellationToken);
        return payload?.Id;
    }

    public async Task<Guid?> ScheduleAssignmentAsync(Guid courseId, ScheduleAssignmentRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync($"/courses/{courseId}/assignments", request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var payload = await response.Content.ReadFromJsonAsync<CreatedResponse>(cancellationToken: cancellationToken);
        return payload?.Id;
    }

    private record CreatedResponse(Guid Id);
}
