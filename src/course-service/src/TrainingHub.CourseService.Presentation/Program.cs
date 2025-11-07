using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.ML;
using Microsoft.ML.Data;
using FluentValidation;
using System.Linq;
using System.Threading;
using Serilog;
using Serilog.Context;
using TrainingHub.CourseService.Application;
using TrainingHub.CourseService.Application.Assignments;
using TrainingHub.CourseService.Application.Courses;
using TrainingHub.CourseService.Domain.Entities;
using TrainingHub.CourseService.Infrastructure;
using TrainingHub.CourseService.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) =>
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.WithProperty("Application", "course-service"));

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<MlPredictionService>();

var app = builder.Build();

// Ensure database is created and seed demo data with simple retry to handle
// the case where PostgreSQL is not yet ready when the service starts.
var logger = app.Logger;
const int maxAttempts = 5;
const int delaySeconds = 5;

for (var attempt = 1; attempt <= maxAttempts; attempt++)
{
    try
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CourseDbContext>();

        dbContext.Database.EnsureCreated();

        // Clean up legacy English demo data so Russian demo data can be seeded.
        var legacyCourses = dbContext.Courses
            .Where(c => c.Title == "Intro to Microservices" || c.Title == "Machine Learning Basics")
            .ToList();
        if (legacyCourses.Count > 0)
        {
            var legacyCourseIds = legacyCourses.Select(c => c.Id).ToList();
            var legacyAssignments = dbContext.Set<Assignment>()
                .Where(a => legacyCourseIds.Contains(a.CourseId))
                .ToList();

            dbContext.RemoveRange(legacyAssignments);
            dbContext.Courses.RemoveRange(legacyCourses);
            dbContext.SaveChanges();
        }

        var demoCourses = new[]
        {
            new
            {
                Title = "Введение в микросервисы",
                Description = "Основы микросервисной архитектуры и обмена сообщениями в TrainingHub.",
                StartsAt = DateTime.Today.AddDays(-7),
                EndsAt = DateTime.Today.AddDays(30),
                AssignmentTitle = "Спроектировать API сервиса курсов",
                AssignmentDescription = "Спроектируйте и реализуйте минимальный API для управления курсами и заданиями.",
                AssignmentDueOffsetDays = 3
            },
            new
            {
                Title = "Основы машинного обучения",
                Description = "Базовые концепции ML, которые используются в аналитике TrainingHub.",
                StartsAt = DateTime.Today.AddDays(-14),
                EndsAt = DateTime.Today.AddDays(21),
                AssignmentTitle = "Построить первую ML‑модель",
                AssignmentDescription = "Подготовьте обучающую выборку и обучите простую модель классификации.",
                AssignmentDueOffsetDays = 5
            },
            new
            {
                Title = "Продвинутая микросервисная архитектура",
                Description = "Шаблоны взаимодействия сервисов, саги и идемпотентность в распределённых системах.",
                StartsAt = DateTime.Today.AddDays(-21),
                EndsAt = DateTime.Today.AddDays(14),
                AssignmentTitle = "Спроектировать сагу для цепочки сервисов",
                AssignmentDescription = "Опишите шаги саги и реализуйте координацию в стиле Choreography или Orchestration.",
                AssignmentDueOffsetDays = 7
            },
            new
            {
                Title = "Мониторинг и логирование",
                Description = "Практика использования централизованного логирования и дашбордов для TrainingHub.",
                StartsAt = DateTime.Today.AddDays(-3),
                EndsAt = DateTime.Today.AddDays(45),
                AssignmentTitle = "Настроить централизованное логирование",
                AssignmentDescription = "Соберите логи нескольких сервисов и выведите ключевые метрики на дашборд.",
                AssignmentDueOffsetDays = 2
            },
            new
            {
                Title = "Очереди сообщений и RabbitMQ",
                Description = "Как организовать надёжную доставку уведомлений и событий в платформе.",
                StartsAt = DateTime.Today.AddDays(-10),
                EndsAt = DateTime.Today.AddDays(35),
                AssignmentTitle = "Смоделировать поток уведомлений через RabbitMQ",
                AssignmentDescription = "Настройте обмен, очередь и подпишитесь на события расписания заданий.",
                AssignmentDueOffsetDays = 4
            }
        };

        var addedAnyDemoCourse = false;

        foreach (var demo in demoCourses)
        {
            var existingCourse = dbContext.Courses
                .Include(c => c.Assignments)
                .FirstOrDefault(c => c.Title == demo.Title);

            if (existingCourse is not null)
            {
                // If course exists but has no assignments, add the primary demo assignment.
                if (!existingCourse.Assignments.Any())
                {
                    var assignment = new Assignment(
                        existingCourse.Id,
                        demo.AssignmentTitle,
                        DateTime.Today.AddDays(demo.AssignmentDueOffsetDays),
                        demo.AssignmentDescription);

                    existingCourse.AddAssignment(assignment);
                    addedAnyDemoCourse = true;
                }

                continue;
            }

            var course = new Course(
                demo.Title,
                demo.Description,
                demo.StartsAt,
                demo.EndsAt);

            var courseAssignment = new Assignment(
                course.Id,
                demo.AssignmentTitle,
                DateTime.Today.AddDays(demo.AssignmentDueOffsetDays),
                demo.AssignmentDescription);

            // Mark one seeded assignment as completed so ML training data contains both classes (Completed=true/false).
            if (demo.Title == "Основы машинного обучения")
            {
                courseAssignment.MarkCompleted();
            }

            course.AddAssignment(courseAssignment);
            dbContext.Courses.Add(course);
            addedAnyDemoCourse = true;
        }

        if (addedAnyDemoCourse)
        {
            dbContext.SaveChanges();
        }

        break;
    }
    catch (Exception ex) when (attempt < maxAttempts)
    {
        logger.LogWarning(ex,
            "Failed to initialize database (attempt {Attempt}/{MaxAttempts}). Waiting {DelaySeconds} seconds before retry.",
            attempt, maxAttempts, delaySeconds);
        Thread.Sleep(TimeSpan.FromSeconds(delaySeconds));
    }
}

// always expose Swagger in this service (even in Docker/Production) for easier testing
app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/swagger/", () => Results.Redirect("/swagger"));

app.MapPost("/courses", async (CreateCourseRequest request, IMediator mediator, CancellationToken ct) =>
    {
        try
        {
            var command = new CreateCourseCommand(request.Title, request.Description, request.StartsAt, request.EndsAt);
            var courseId = await mediator.Send(command, ct);
            return Results.Created($"/courses/{courseId}", new { id = courseId });
        }
        catch (ValidationException ex)
        {
            var errors = ex.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            return Results.ValidationProblem(errors);
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    })
    .WithName("CreateCourse")
    .Produces(StatusCodes.Status201Created)
    .ProducesProblem(StatusCodes.Status400BadRequest);

app.MapGet("/courses/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
    {
        var result = await mediator.Send(new GetCourseQuery(id), ct);
        return result is not null ? Results.Ok(result) : Results.NotFound();
    })
    .WithName("GetCourse")
    .Produces(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status404NotFound);

app.MapGet("/courses/{id:guid}/assignments", async (Guid id, IMediator mediator, CancellationToken ct) =>
    {
        var assignments = await mediator.Send(new GetAssignmentsQuery(id), ct);
        return Results.Ok(assignments);
    })
    .WithName("GetAssignments")
    .Produces(StatusCodes.Status200OK);

app.MapPost("/courses/{id:guid}/assignments", async (Guid id, ScheduleAssignmentRequest request, IMediator mediator, CancellationToken ct) =>
    {
        try
        {
            var command = new ScheduleAssignmentCommand(id, request.Title, request.DueDate, request.Description);
            var assignmentId = await mediator.Send(command, ct);
            return Results.Created($"/courses/{id}/assignments/{assignmentId}", new { id = assignmentId });
        }
        catch (InvalidOperationException ex)
        {
            return Results.NotFound(new { message = ex.Message });
        }
    })
    .WithName("ScheduleAssignment")
    .Produces(StatusCodes.Status201Created)
    .Produces(StatusCodes.Status404NotFound);

app.MapGet("/courses", async ([AsParameters] GetCoursesRequest request, IMediator mediator, CancellationToken ct) =>
    {
        if (request.Popular.HasValue && request.Popular.Value)
        {
            var popular = await mediator.Send(new GetPopularCoursesQuery(request.Take ?? 5), ct);
            return Results.Ok(popular);
        }

        return Results.BadRequest("Specify query parameters (e.g. popular=true)");
    })
    .WithName("GetCourses")
    .Produces(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status400BadRequest);

app.MapGet("/analytics/assignments/training", async (IMediator mediator, CancellationToken ct) =>
    {
        var data = await mediator.Send(new GetAssignmentTrainingDataQuery(), ct);
        return Results.Ok(data);
    })
    .WithName("GetAssignmentTrainingData")
    .Produces(StatusCodes.Status200OK);

app.MapPost("/analytics/assignments/predict", (PredictAssignmentRequest request, MlPredictionService predictor) =>
    {
        var input = new AssignmentPredictionInput
        {
            CourseId = request.CourseId.ToString("N"),
            DaysUntilDue = (float)(request.DueDate - DateTime.UtcNow).TotalDays
        };

        if (!predictor.TryPredict(input, out var prediction, out var modelLastWriteUtc, out var error))
        {
            return Results.Problem(
                detail: error,
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "ML model is not ready");
        }

        return Results.Ok(new PredictAssignmentResponse(
            PredictedCompleted: prediction.PredictedLabel,
            Score: prediction.Score,
            Probability: prediction.Probability,
            ModelLastWriteUtc: modelLastWriteUtc));
    })
    .WithName("PredictAssignmentCompletion")
    .Produces<PredictAssignmentResponse>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

app.MapGet("/demo/logs", (ILogger<Program> logger) =>
    {
        using (LogContext.PushProperty("DemoCorrelationId", Guid.NewGuid()))
        {
            logger.LogInformation("Demo log event: {Action} {@Meta}", "demo-start", new { AtUtc = DateTime.UtcNow, Source = "course-service" });
            logger.LogWarning("Demo warning: {WarningCode} {@Details}", "DEMO_WARN", new { Reason = "This is an intentional warning for Seq demo" });

            try
            {
                var zero = 0;
                var _ = 123 / zero;
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

app.MapGet("/demo/redis", async (IMediator mediator, ILogger<Program> logger, int? take, CancellationToken ct) =>
    {
        var requestedTake = take.GetValueOrDefault(5);
        if (requestedTake <= 0)
        {
            requestedTake = 5;
        }

        using (LogContext.PushProperty("DemoCorrelationId", Guid.NewGuid()))
        {
            logger.LogInformation("Redis demo started (take={Take})", requestedTake);
            var first = await mediator.Send(new GetPopularCoursesQuery(requestedTake), ct);
            var second = await mediator.Send(new GetPopularCoursesQuery(requestedTake), ct);
            logger.LogInformation("Redis demo finished (take={Take})", requestedTake);

            var firstPreview = first.Select(c => new { c.Id, c.Title }).ToArray();
            var secondPreview = second.Select(c => new { c.Id, c.Title }).ToArray();

            return Results.Ok(new
            {
                ok = true,
                take = requestedTake,
                firstCount = firstPreview.Length,
                secondCount = secondPreview.Length,
                first = firstPreview,
                second = secondPreview
            });
        }
    })
    .WithName("DemoRedis")
    .Produces(StatusCodes.Status200OK);

app.MapGet("/demo/rabbitmq", async (
        IMediator mediator,
        IAssignmentEventPublisher publisher,
        ILogger<Program> logger,
        Guid? courseId,
        string? title,
        CancellationToken ct) =>
    {
        var resolvedCourseId = courseId;
        if (!resolvedCourseId.HasValue)
        {
            var popular = await mediator.Send(new GetPopularCoursesQuery(1), ct);
            resolvedCourseId = popular.FirstOrDefault()?.Id;
        }

        if (!resolvedCourseId.HasValue)
        {
            return Results.NotFound(new { message = "No courses found to attach demo notification" });
        }

        var now = DateTime.UtcNow;
        var @event = new AssignmentScheduledEvent(
            AssignmentId: Guid.NewGuid(),
            CourseId: resolvedCourseId.Value,
            Title: title ?? $"RabbitMQ demo notification ({now:O})",
            DueDate: now.AddMinutes(10),
            CreatedAtUtc: now);

        using (LogContext.PushProperty("DemoCorrelationId", Guid.NewGuid()))
        {
            logger.LogInformation("RabbitMQ demo publishing {@Event}", @event);
            await publisher.PublishAssignmentScheduledAsync(@event, ct);
            logger.LogInformation("RabbitMQ demo published {@Event}", @event);
        }

        return Results.Ok(new { ok = true, courseId = resolvedCourseId.Value, assignmentId = @event.AssignmentId, @event.Title });
    })
    .WithName("DemoRabbitMq")
    .Produces(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status404NotFound);

app.MapGet("/health", () => Results.Ok("ok"))
    .WithName("Health")
    .Produces(StatusCodes.Status200OK);

app.Run();

public record CreateCourseRequest(string Title, string Description, DateTime StartsAt, DateTime EndsAt);

public record GetCoursesRequest(bool? Popular, int? Take);

public record ScheduleAssignmentRequest(string Title, DateTime DueDate, string Description);

public record PredictAssignmentRequest(Guid CourseId, DateTime DueDate);

public record PredictAssignmentResponse(bool PredictedCompleted, float Score, float Probability, DateTime? ModelLastWriteUtc);

public class AssignmentPredictionInput
{
    public string CourseId { get; set; } = string.Empty;

    public float DaysUntilDue { get; set; }
}

public class AssignmentPredictionOutput
{
    [ColumnName("PredictedLabel")]
    public bool PredictedLabel { get; set; }

    public float Score { get; set; }

    public float Probability { get; set; }
}

public class MlPredictionService
{
    private readonly ILogger<MlPredictionService> _logger;
    private readonly string _modelPath;
    private readonly MLContext _mlContext;
    private readonly object _gate = new();
    private ITransformer? _model;
    private DateTime? _modelLastWriteUtc;

    public MlPredictionService(ILogger<MlPredictionService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _modelPath = configuration["ModelInference:ModelPath"] ?? "/app/models/assignment-model.zip";
        _mlContext = new MLContext(seed: 42);
    }

    public bool TryPredict(
        AssignmentPredictionInput input,
        out AssignmentPredictionOutput prediction,
        out DateTime? modelLastWriteUtc,
        out string? error)
    {
        prediction = new AssignmentPredictionOutput();
        modelLastWriteUtc = null;
        error = null;

        if (!File.Exists(_modelPath))
        {
            error = $"Model file not found at '{_modelPath}'";
            return false;
        }

        try
        {
            var lastWriteUtc = File.GetLastWriteTimeUtc(_modelPath);

            lock (_gate)
            {
                if (_model is null || _modelLastWriteUtc != lastWriteUtc)
                {
                    using var stream = File.OpenRead(_modelPath);
                    _model = _mlContext.Model.Load(stream, out _);
                    _modelLastWriteUtc = lastWriteUtc;
                    _logger.LogInformation("Loaded ML model from {Path} (lastWriteUtc={LastWriteUtc})", _modelPath, lastWriteUtc);
                }
            }

            var engine = _mlContext.Model.CreatePredictionEngine<AssignmentPredictionInput, AssignmentPredictionOutput>(_model!);
            prediction = engine.Predict(input);
            modelLastWriteUtc = _modelLastWriteUtc;
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run ML prediction");
            error = ex.Message;
            return false;
        }
    }
}
