namespace TrainingHub.CourseService.Worker.ML;

public class ModelOptions
{
    public string TrainingDataEndpoint { get; set; } = "/analytics/assignments/training";

    public string CourseServiceBaseAddress { get; set; } = "https://localhost:7079";

    public string ModelOutputPath { get; set; } = "models/assignment-model.zip";

    public int RefreshHours { get; set; } = 6;
}
