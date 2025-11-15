using System.Net.Http.Json;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.Extensions.Options;

namespace TrainingHub.CourseService.Worker.ML;

public class MlTrainerWorker : BackgroundService
{
    private readonly ILogger<MlTrainerWorker> _logger;
    private readonly ModelOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly MLContext _mlContext;
    private int _consecutiveFailures;
    private DateTimeOffset _lastFailureLogAt = DateTimeOffset.MinValue;

    public MlTrainerWorker(ILogger<MlTrainerWorker> logger, IOptions<ModelOptions> options, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _options = options.Value;
        _httpClientFactory = httpClientFactory;
        _mlContext = new MLContext(seed: 42);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var trained = await TrainModelAsync(stoppingToken);
            if (trained)
            {
                _consecutiveFailures = 0;
            }

            var delay = trained
                ? TimeSpan.FromHours(_options.RefreshHours)
                : GetFailureBackoffDelay();

            await Task.Delay(delay, stoppingToken);
        }
    }

    private TimeSpan GetFailureBackoffDelay()
    {
        _consecutiveFailures = Math.Min(_consecutiveFailures + 1, 10);
        var seconds = Math.Min(300, Math.Pow(2, _consecutiveFailures));
        return TimeSpan.FromSeconds(seconds);
    }

    private async Task<bool> TrainModelAsync(CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("ML worker fetching training data at {Time}", DateTimeOffset.UtcNow);
            var client = _httpClientFactory.CreateClient("CourseService");
            var assignments = await client.GetFromJsonAsync<List<AssignmentTrainingDto>>(_options.TrainingDataEndpoint, ct) ?? new();

            if (assignments.Count == 0)
            {
                _logger.LogWarning("No training data available");
                return false;
            }

            if (!assignments.Any(a => a.Completed) || !assignments.Any(a => !a.Completed))
            {
                _logger.LogWarning("Training data does not contain both classes (completed=true/false)");
                return false;
            }

            var positiveCount = assignments.Count(a => a.Completed);
            var negativeCount = assignments.Count - positiveCount;

            var data = assignments.Select(a => new AssignmentTrainingModel
            {
                CourseId = a.CourseId.ToString("N"),
                DaysUntilDue = (float)(a.DueDate - DateTime.UtcNow).TotalDays,
                Completed = a.Completed
            }).ToList();

            var dataView = _mlContext.Data.LoadFromEnumerable(data);

            var pipeline = _mlContext.Transforms.Categorical.OneHotHashEncoding(
                    outputColumnName: "CourseIdEncoded",
                    inputColumnName: nameof(AssignmentTrainingModel.CourseId))
                .Append(_mlContext.Transforms.Concatenate("Features", nameof(AssignmentTrainingModel.DaysUntilDue), "CourseIdEncoded"))
                .Append(_mlContext.BinaryClassification.Trainers.SdcaLogisticRegression(labelColumnName: nameof(AssignmentTrainingModel.Completed)));

            var model = pipeline.Fit(dataView);

            var folds = Math.Min(5, Math.Min(assignments.Count, Math.Min(positiveCount, negativeCount)));
            if (folds >= 2)
            {
                try
                {
                    var metrics = _mlContext.BinaryClassification.CrossValidate(
                        dataView,
                        pipeline,
                        numberOfFolds: folds,
                        labelColumnName: nameof(AssignmentTrainingModel.Completed));

                    var avgAccuracy = metrics.Average(m => m.Metrics.Accuracy);
                    _logger.LogInformation("ML model cross-validated accuracy: {Accuracy}", avgAccuracy);
                }
                catch (Exception ex)
                {
                    _logger.LogInformation("ML model cross-validation failed; continuing without metrics. Reason: {Reason}", ex.Message);
                }
            }
            else
            {
                _logger.LogInformation("Not enough samples for cross-validation, skipping metrics");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(_options.ModelOutputPath)!);
            _mlContext.Model.Save(model, dataView.Schema, _options.ModelOutputPath);
            _logger.LogInformation("Saved ML model to {Path}", _options.ModelOutputPath);
            return true;
        }
        catch (Exception ex)
        {
            var now = DateTimeOffset.UtcNow;
            if (now - _lastFailureLogAt > TimeSpan.FromMinutes(1))
            {
                _lastFailureLogAt = now;
                _logger.LogError(ex, "Failed to train ML model");
            }
            else
            {
                _logger.LogWarning("Failed to train ML model (suppressed details due to frequent errors)");
            }
            return false;
        }
    }

    private record AssignmentTrainingDto(Guid CourseId, Guid AssignmentId, DateTime DueDate, bool Completed);

    private class AssignmentTrainingModel
    {
        public string CourseId { get; set; } = string.Empty;
        public float DaysUntilDue { get; set; }
        public bool Completed { get; set; }
    }
}
