using System.Text.Json;
using System.Text.Json.Serialization;
using System.Reflection;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using TrainingHub.CourseService.Application.Courses;
using TrainingHub.CourseService.Domain.Entities;

namespace TrainingHub.CourseService.Infrastructure.Caching;

public class RedisCourseCache : ICourseCache
{
    private const string CacheKeyPrefix = "traininghub:courses:popular:";
    private readonly IDistributedCache _cache;
    private readonly ILogger<RedisCourseCache> _logger;
    private readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.Web)
    {
        ReferenceHandler = ReferenceHandler.IgnoreCycles
    };

    public RedisCourseCache(IDistributedCache cache, ILogger<RedisCourseCache> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<IReadOnlyCollection<Course>> GetPopularAsync(int take, CancellationToken cancellationToken)
    {
        var key = BuildKey(take);
        var cached = await _cache.GetStringAsync(key, cancellationToken);
        if (string.IsNullOrWhiteSpace(cached))
        {
            _logger.LogInformation("Redis cache miss: {CacheKey} (take={Take})", key, take);
            return Array.Empty<Course>();
        }

        try
        {
            var cachedCourses = JsonSerializer.Deserialize<List<CachedCourse>>(cached, _serializerOptions);
            var result = (cachedCourses ?? new List<CachedCourse>()).Select(MapFromCache).ToList();
            _logger.LogInformation("Redis cache hit: {CacheKey} (take={Take}, items={ItemCount})", key, take, result.Count);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogInformation("Redis cache deserialization failed: {CacheKey}. Reason: {Reason}", key, ex.Message);
            await _cache.RemoveAsync(key, cancellationToken);
            // If deserialization fails (e.g. due to schema changes), ignore the cached value
            // so that callers can fall back to the database and repopulate the cache.
            return Array.Empty<Course>();
        }
    }

    public async Task SetPopularAsync(int take, IReadOnlyCollection<Course> courses, TimeSpan ttl, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Serialize(courses.Select(MapToCache).ToList(), _serializerOptions);
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl
        };

        var key = BuildKey(take);
        await _cache.SetStringAsync(key, payload, options, cancellationToken);
        _logger.LogInformation("Redis cache set: {CacheKey} (take={Take}, items={ItemCount}, ttlMinutes={TtlMinutes})",
            key,
            take,
            courses.Count,
            ttl.TotalMinutes);
    }

    private static string BuildKey(int take) => $"{CacheKeyPrefix}{take}";

    private static CachedCourse MapToCache(Course course)
    {
        return new CachedCourse(
            course.Id,
            course.Title,
            course.Description,
            course.CreatedAt,
            course.Period.Start,
            course.Period.End);
    }

    private static Course MapFromCache(CachedCourse cached)
    {
        var course = new Course(cached.Title, cached.Description, cached.StartsAtUtc, cached.EndsAtUtc);

        SetAutoProperty(course, nameof(Course.Id), cached.Id);
        SetAutoProperty(course, nameof(Course.CreatedAt), cached.CreatedAtUtc);

        return course;
    }

    private static void SetAutoProperty<TValue>(Course course, string propertyName, TValue value)
    {
        var property = typeof(Course).GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (property is null)
        {
            return;
        }

        property.SetValue(course, value);
    }

    private sealed record CachedCourse(
        Guid Id,
        string Title,
        string Description,
        DateTime CreatedAtUtc,
        DateTime StartsAtUtc,
        DateTime EndsAtUtc);
}
