using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using System.Globalization;
using TrainingHub.BlazorClient;
using TrainingHub.BlazorClient.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

var apiGatewayBaseAddress = builder.Configuration["Services:ApiGateway"] ?? builder.HostEnvironment.BaseAddress;

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiGatewayBaseAddress) });
builder.Services.AddScoped<ICourseApiClient, CourseApiClient>();
builder.Services.AddScoped<INotificationHubClient, NotificationHubClient>();

var host = builder.Build();

var js = host.Services.GetRequiredService<IJSRuntime>();
var savedCulture = await js.InvokeAsync<string>("blazorCulture.get");
var culture = !string.IsNullOrWhiteSpace(savedCulture)
    ? new CultureInfo(savedCulture)
    : new CultureInfo("ru-RU");

CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

await host.RunAsync();
