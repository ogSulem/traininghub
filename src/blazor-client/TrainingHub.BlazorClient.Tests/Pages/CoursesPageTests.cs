using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using TrainingHub.BlazorClient.Pages;
using TrainingHub.BlazorClient.Services;
using TrainingHub.BlazorClient.Shared;
using Xunit;

namespace TrainingHub.BlazorClient.Tests.Pages;

public class CoursesPageTests : TestContext
{
    [Fact]
    public void CoursesPage_ShowsCourseList()
    {
        Services.AddSingleton<ICourseApiClient>(new FakeCourseApiClient());
        Services.AddSingleton<IStringLocalizer<Courses>>(new TestStringLocalizer<Courses>());

        var cut = RenderComponent<Courses>();

        Assert.Contains("Available courses", cut.Markup);
        Assert.Contains("Demo Course", cut.Markup);
    }

    private sealed class FakeCourseApiClient : ICourseApiClient
    {
        private readonly CourseDto _course = new(Guid.NewGuid(), "Demo Course", "Desc");

        public Task<Guid?> CreateCourseAsync(CreateCourseRequest request, CancellationToken cancellationToken = default) => Task.FromResult<Guid?>(Guid.NewGuid());

        public Task<CourseDetailsDto?> GetCourseAsync(Guid courseId, CancellationToken cancellationToken = default)
            => Task.FromResult<CourseDetailsDto?>(new CourseDetailsDto(courseId, "Demo Course", "Desc",
                new[] { new AssignmentDto(Guid.NewGuid(), courseId, "Lab", DateTime.Today, "Lab desc", 0) }));

        public Task<IReadOnlyList<AssignmentDto>> GetAssignmentsAsync(Guid courseId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<AssignmentDto>>(Array.Empty<AssignmentDto>());

        public Task<IReadOnlyList<CourseDto>> GetPopularCoursesAsync(int take = 5, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<CourseDto>>(new[] { _course });

        public Task<Guid?> ScheduleAssignmentAsync(Guid courseId, ScheduleAssignmentRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult<Guid?>(Guid.NewGuid());
    }

    private sealed class TestStringLocalizer<T> : IStringLocalizer<T>
    {
        public LocalizedString this[string name]
            => new(name, name switch
            {
                "Title" => "Courses",
                "AvailableCourses" => "Available courses",
                "DetailsHeader" => "Details",
                "AssignmentsHeader" => "Assignments",
                "SelectHint" => "Select a course",
                "EmptyCourses" => "Empty",
                "AssignmentTitle" => "Title",
                "DueDate" => "Due Date",
                "Status" => "Status",
                _ => name
            });

        public LocalizedString this[string name, params object[] arguments] => this[name];

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => throw new NotSupportedException();

        public IStringLocalizer WithCulture(System.Globalization.CultureInfo culture) => this;
    }
}
