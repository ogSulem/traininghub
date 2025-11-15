using TrainingHub.CourseService.Worker.ML;
using Microsoft.Extensions.Options;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSerilog((services, configuration) =>
    configuration
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services)
        .Enrich.WithProperty("Application", "course-service-worker-ml"));

builder.Services.AddOptions<ModelOptions>()
    .Bind(builder.Configuration.GetSection("Model"));

builder.Services.AddHttpClient("CourseService", (sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<ModelOptions>>().Value;
    client.BaseAddress = new Uri(options.CourseServiceBaseAddress);
});

builder.Services.AddHostedService<MlTrainerWorker>();

var host = builder.Build();
host.Run();
