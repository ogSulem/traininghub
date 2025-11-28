using System.Globalization;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;
using Serilog;
using Serilog.Context;
using TrainingHub.ApiGateway.Contracts.Requests;
using TrainingHub.ApiGateway.Contracts.Responses;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) =>
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.WithProperty("Application", "api-gateway"));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddLocalization();
builder.Services.AddSignalR();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var courseServiceBase = builder.Configuration["Services:CourseService"] ?? "https://localhost:7079";
var retryCount = builder.Configuration.GetValue("Polly:RetryCount", 3);
var retryDelay = builder.Configuration.GetValue("Polly:RetryDelaySeconds", 2);

builder.Services.AddHttpClient("CourseService", client =>
    {
        client.BaseAddress = new Uri(courseServiceBase);
    })
    .AddPolicyHandler(HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(retryCount, attempt => TimeSpan.FromSeconds(retryDelay * attempt)));

var app = builder.Build();

var supportedCultures = new[] { new CultureInfo("en"), new CultureInfo("ru") };
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture("en"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
});

// always expose Swagger in the gateway (even in Docker/Production) for easier testing
app.UseSwagger(); 
app.UseSwaggerUI();

app.MapGet("/swagger/", () => Results.Redirect("/swagger"));

app.UseCors();

app.MapPost("/courses", async (CreateCourseRequest request, IHttpClientFactory httpClientFactory, CancellationToken ct) =>
{
    var client = httpClientFactory.CreateClient("CourseService");
    var response = await client.PostAsJsonAsync("/courses", request, ct);
    if (response.IsSuccessStatusCode)
    {
        var payload = await response.Content.ReadFromJsonAsync<object>(cancellationToken: ct);
        return Results.Created(response.Headers.Location ?? new Uri($"/courses", UriKind.Relative), payload);
    }

    try
    {
        var errorBody = await response.Content.ReadFromJsonAsync<object>(cancellationToken: ct);
        if (errorBody is not null)
        {
            return Results.Json(errorBody, statusCode: (int)response.StatusCode);
        }
    }
    catch (System.Text.Json.JsonException)
    {
        // Body is not JSON (or empty) - fall back to plain text or bare status code
        var raw = await response.Content.ReadAsStringAsync(ct);
        if (!string.IsNullOrWhiteSpace(raw))
        {
            return Results.Problem(raw, statusCode: (int)response.StatusCode);
        }
    }

    return Results.StatusCode((int)response.StatusCode);
})
.WithName("GatewayCreateCourse")
.Produces(StatusCodes.Status201Created)
.ProducesProblem(StatusCodes.Status500InternalServerError);

app.MapGet("/courses", async (bool? popular, int? take, IHttpClientFactory httpClientFactory, CancellationToken ct) =>
{
    var client = httpClientFactory.CreateClient("CourseService");

    var queryParts = new List<string>();
    if (popular.HasValue)
    {
        queryParts.Add($"popular={popular.Value.ToString().ToLowerInvariant()}");
    }
    if (take.HasValue)
    {
        queryParts.Add($"take={take.Value}");
    }

    var path = "/courses";
    if (queryParts.Count > 0)
    {
        path += "?" + string.Join("&", queryParts);
    }

    HttpResponseMessage response;
    try
    {
        response = await client.GetAsync(path, ct);
    }
    catch (HttpRequestException ex)
    {
        return Results.Problem(ex.Message, statusCode: StatusCodes.Status502BadGateway);
    }

    if (response.IsSuccessStatusCode)
    {
        var payload = await response.Content.ReadFromJsonAsync<object>(cancellationToken: ct);
        return Results.Ok(payload);
    }

    try
    {
        var errorBody = await response.Content.ReadFromJsonAsync<object>(cancellationToken: ct);
        if (errorBody is not null)
        {
            return Results.Json(errorBody, statusCode: (int)response.StatusCode);
        }
    }
    catch (System.Text.Json.JsonException)
    {
        var raw = await response.Content.ReadAsStringAsync(ct);
        if (!string.IsNullOrWhiteSpace(raw))
        {
            return Results.Problem(raw, statusCode: (int)response.StatusCode);
        }
    }

    return Results.StatusCode((int)response.StatusCode);
})
.WithName("GatewayGetCourses")
.Produces(StatusCodes.Status200OK)
.ProducesProblem(StatusCodes.Status400BadRequest)
.ProducesProblem(StatusCodes.Status500InternalServerError);

app.MapGet("/courses/{id:guid}", async (Guid id, IHttpClientFactory httpClientFactory, CancellationToken ct) =>
{
    var client = httpClientFactory.CreateClient("CourseService");
    var response = await client.GetAsync($"/courses/{id}", ct);
    return response.IsSuccessStatusCode
        ? Results.Ok(await response.Content.ReadFromJsonAsync<CourseResponse>(cancellationToken: ct))
        : Results.NotFound();
})
.WithName("GatewayGetCourse")
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound);

app.MapPost("/courses/{id:guid}/assignments", async (Guid id, ScheduleAssignmentRequest request, IHttpClientFactory httpClientFactory, CancellationToken ct) =>
{
    var client = httpClientFactory.CreateClient("CourseService");
    var response = await client.PostAsJsonAsync($"/courses/{id}/assignments", request, ct);

    if (response.IsSuccessStatusCode)
    {
        var payload = await response.Content.ReadFromJsonAsync<object>(cancellationToken: ct);
        return Results.Created(response.Headers.Location ?? new Uri($"/courses/{id}/assignments", UriKind.Relative), payload);
    }

    try
    {
        var errorBody = await response.Content.ReadFromJsonAsync<object>(cancellationToken: ct);
        if (errorBody is not null)
        {
            return Results.Json(errorBody, statusCode: (int)response.StatusCode);
        }
    }
    catch (System.Text.Json.JsonException)
    {
        var raw = await response.Content.ReadAsStringAsync(ct);
        if (!string.IsNullOrWhiteSpace(raw))
        {
            return Results.Problem(raw, statusCode: (int)response.StatusCode);
        }
    }

    return Results.StatusCode((int)response.StatusCode);
})
.WithName("GatewayScheduleAssignment")
.Produces(StatusCodes.Status201Created)
.ProducesProblem(StatusCodes.Status500InternalServerError);

app.MapGet("/courses/{id:guid}/assignments", async (Guid id, IHttpClientFactory httpClientFactory, CancellationToken ct) =>
{
    var client = httpClientFactory.CreateClient("CourseService");
    var response = await client.GetAsync($"/courses/{id}/assignments", ct);
    return response.IsSuccessStatusCode
        ? Results.Ok(await response.Content.ReadFromJsonAsync<object>(cancellationToken: ct))
        : Results.NotFound();
})
.WithName("GatewayGetAssignments")
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound);

app.MapHub<NotificationsHub>("/hub/notifications");

app.MapPost("/internal/notifications/assignments", async (AssignmentNotification notification, IHubContext<NotificationsHub> hub, ILogger<NotificationsHub> logger, CancellationToken ct) =>
{
    logger.LogInformation("Broadcasting assignment notification {@Notification}", notification);
    await hub.Clients.All.SendAsync("AssignmentScheduled", notification, ct);
    return Results.Accepted();
})
.WithName("GatewayInternalAssignmentNotification")
.Produces(StatusCodes.Status202Accepted);

app.MapGet("/demo/logs", (ILogger<Program> logger) =>
    {
        using (LogContext.PushProperty("DemoCorrelationId", Guid.NewGuid()))
        {
            logger.LogInformation("Demo log event: {Action} {@Meta}", "demo-start", new { AtUtc = DateTime.UtcNow, Source = "api-gateway" });
            logger.LogWarning("Demo warning: {WarningCode} {@Details}", "DEMO_WARN", new { Reason = "This is an intentional warning for Seq demo" });

            try
            {
                throw new InvalidOperationException("Intentional demo exception (API Gateway)");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Demo error: {ErrorCode}", "DEMO_ERROR");
            }

            logger.LogInformation("Demo log event: {Action}", "demo-finish");
        }

        return Results.Ok(new { ok = true });
    })
    .WithName("DemoLogs")
    .Produces(StatusCodes.Status200OK);

app.MapGet("/health", () => Results.Ok("ok"))
    .WithName("Health")
    .Produces(StatusCodes.Status200OK);

app.Run();

public class NotificationsHub : Hub
{
}

public record AssignmentNotification(Guid CourseId, Guid AssignmentId, string Title, DateTime DueDate);

public partial class Program
{
}
