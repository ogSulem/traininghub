using System.Text.Json;
using RichardSzalay.MockHttp;
using TrainingHub.BlazorClient.Services;
using TrainingHub.BlazorClient.Shared;
using Xunit;

namespace TrainingHub.BlazorClient.Tests.Services;

public class CourseApiClientTests
{
    private readonly MockHttpMessageHandler _mockHttp = new();

    [Fact]
    public async Task GetPopularCoursesAsync_ReturnsCourses()
    {
        // Arrange
        var expected = new[] { new CourseDto(Guid.NewGuid(), "Sample", "Desc") };
        _mockHttp.When(HttpMethod.Get, "http://localhost/courses*")
            .Respond("application/json", JsonSerializer.Serialize(expected));

        var client = new CourseApiClient(new HttpClient(_mockHttp) { BaseAddress = new Uri("http://localhost") });

        // Act
        var result = await client.GetPopularCoursesAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal(expected[0].Title, result[0].Title);
    }

    [Fact]
    public async Task CreateCourseAsync_WhenServerReturnsId_ParsesResult()
    {
        var id = Guid.NewGuid();
        _mockHttp.When(HttpMethod.Post, "http://localhost/courses")
            .Respond("application/json", JsonSerializer.Serialize(new { id }));

        var client = new CourseApiClient(new HttpClient(_mockHttp) { BaseAddress = new Uri("http://localhost") });

        var result = await client.CreateCourseAsync(new CreateCourseRequest("Title", "Desc", DateTime.UtcNow, DateTime.UtcNow.AddDays(1)));

        Assert.Equal(id, result);
    }
}
