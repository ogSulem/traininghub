using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using TrainingHub.BlazorClient.Pages;
using TrainingHub.BlazorClient.Services;
using TrainingHub.BlazorClient.Shared;
using Xunit;

namespace TrainingHub.BlazorClient.Tests.Pages;

public class HomePageTests : TestContext
{
    [Fact]
    public void HomePage_RendersDashboardTitle()
    {
        Services.AddSingleton<ICourseApiClient>(new FakeCourseApiClient());
        Services.AddSingleton<INotificationHubClient>(new FakeNotificationHubClient());
        Services.AddSingleton<IStringLocalizer<Home>>(new TestStringLocalizer<Home>());

        var cut = RenderComponent<Home>();

        Assert.Contains("TrainingHub dashboard", cut.Markup);
    }

    private sealed class FakeCourseApiClient : ICourseApiClient
    {
        public Task<Guid?> CreateCourseAsync(CreateCourseRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult<Guid?>(Guid.NewGuid());

        public Task<CourseDetailsDto?> GetCourseAsync(Guid courseId, CancellationToken cancellationToken = default)
            => Task.FromResult<CourseDetailsDto?>(new CourseDetailsDto(courseId, "Test Course", "Description", Array.Empty<AssignmentDto>()));

        public Task<IReadOnlyList<AssignmentDto>> GetAssignmentsAsync(Guid courseId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<AssignmentDto>>(Array.Empty<AssignmentDto>());

        public Task<IReadOnlyList<CourseDto>> GetPopularCoursesAsync(int take = 5, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<CourseDto>>(new[] { new CourseDto(Guid.NewGuid(), "Sample", "Sample description") });

        public Task<Guid?> ScheduleAssignmentAsync(Guid courseId, ScheduleAssignmentRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult<Guid?>(Guid.NewGuid());
    }

    private sealed class FakeNotificationHubClient : INotificationHubClient
    {
        public event Func<AssignmentNotificationDto, Task>? OnAssignmentScheduled
        {
            add { }
            remove { }
        }

        public Task StartAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class TestStringLocalizer<T> : IStringLocalizer<T>
    {
        public LocalizedString this[string name]
            => new(name, name switch
            {
                "DashboardTitle" => "TrainingHub dashboard",
                "HeroSubtitle" => "Subtitle",
                "Refresh" => "Refresh",
                _ => name
            });

        public LocalizedString this[string name, params object[] arguments] => this[name];

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
            => throw new NotSupportedException();

        public IStringLocalizer WithCulture(System.Globalization.CultureInfo culture)
            => this;
    }
}
