using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;
using TrainingHub.BlazorClient.Services;
using TrainingHub.BlazorClient.Shared;
using Xunit;

namespace TrainingHub.BlazorClient.Tests.Services;

public class NotificationHubClientTests
{
    [Fact]
    public void BuildHubUrl_MapsLocalhost5173ToGateway5000()
    {
        var url = NotificationHubClient.BuildHubUrl("http://localhost:5173/");
        Assert.Equal("http://localhost:5000/hub/notifications", url);
    }

    [Fact]
    public void BuildHubUrl_UsesBaseUriWhenNot5173()
    {
        var url = NotificationHubClient.BuildHubUrl("http://example.com/app/");
        Assert.Equal("http://example.com/app/hub/notifications", url);
    }

    private sealed class TestNavigationManager : NavigationManager
    {
        public TestNavigationManager()
        {
            Initialize("http://localhost/", "http://localhost/");
        }

        protected override void NavigateToCore(string uri, NavigationOptions options)
        {
        }
    }
}
