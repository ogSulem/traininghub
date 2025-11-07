using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TrainingHub.CourseService.Application.Courses;
using TrainingHub.CourseService.Application.Assignments;
using TrainingHub.CourseService.Application.Notifications;
using TrainingHub.CourseService.Infrastructure.Caching;
using TrainingHub.CourseService.Infrastructure.Messaging;
using TrainingHub.CourseService.Infrastructure.Notifications;
using TrainingHub.CourseService.Infrastructure.Persistence;
using Microsoft.Extensions.Options;

namespace TrainingHub.CourseService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<CourseDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("TrainingHub")));

        services.AddScoped<ICourseRepository, CourseRepository>();
        services.AddScoped<ICourseCache, RedisCourseCache>();
        services.Configure<RabbitMqOptions>(configuration.GetSection("Messaging:RabbitMQ"));
        services.AddSingleton<IAssignmentEventPublisher, RabbitMqAssignmentEventPublisher>();
        services.AddHostedService<RabbitMqAssignmentEventConsumer>();
        services.Configure<NotificationGatewayOptions>(configuration.GetSection("Services:NotificationGateway"));
        services.AddHttpClient<INotificationDispatcher, NotificationDispatcher>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<NotificationGatewayOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseAddress);
        });

        var redisConnection = configuration.GetConnectionString("Redis") ?? "localhost:6379";
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnection;
            options.InstanceName = "traininghub:";
        });

        // RabbitMQ infrastructure (publisher & consumer) is fully wired here.
        // Quartz scheduling is configured in the separate TrainingHub.CourseService.Worker.Quartz project.

        return services;
    }
}
