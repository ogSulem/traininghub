using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TrainingHub.ApiGateway.Tests;

public class GatewaySmokeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public GatewaySmokeTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Health_ReturnsOk()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Swagger_IsReachable()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/swagger");

        Assert.True(
            response.StatusCode is HttpStatusCode.OK or HttpStatusCode.Redirect,
            $"Unexpected status code: {(int)response.StatusCode} {response.StatusCode}");
    }
}
