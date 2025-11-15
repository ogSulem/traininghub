using Microsoft.Extensions.Options;
using Quartz;
using Serilog;
using TrainingHub.CourseService.Application.Notifications;
using TrainingHub.CourseService.Infrastructure.Notifications;
using TrainingHub.CourseService.Worker.Quartz.Jobs;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSerilog((services, configuration) =>
    configuration
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services)
        .Enrich.WithProperty("Application", "course-service-worker-quartz"));

var courseServiceBaseAddress = builder.Configuration["Services:CourseService"] ?? "https://localhost:7079";
builder.Services.AddHttpClient("CourseService", client =>
{
    client.BaseAddress = new Uri(courseServiceBaseAddress);
});

builder.Services.Configure<NotificationGatewayOptions>(builder.Configuration.GetSection("Services:NotificationGateway"));
builder.Services.AddHttpClient<INotificationDispatcher, NotificationDispatcher>((sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<NotificationGatewayOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseAddress);
});

builder.Services.AddQuartz(q =>
{
    var jobKey = new JobKey("DailyReminderJob");
    var cron = builder.Configuration["Quartz:DailyReminderCron"] ?? "0 0 18 * * ?";
    q.AddJob<DailyReminderJob>(opts => opts.WithIdentity(jobKey));
    q.AddTrigger(opts => opts
        .ForJob(jobKey)
        .WithIdentity("DailyReminderTrigger")
        .WithCronSchedule(cron));
});

builder.Services.AddQuartzHostedService(options =>
{
    options.WaitForJobsToComplete = true;
});

var host = builder.Build();
host.Run();
