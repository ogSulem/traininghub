using TrainingHub.CourseService.Domain.ValueObjects;

namespace TrainingHub.CourseService.Domain.Entities;

public class Course
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public string Title { get; private set; }

    public string Description { get; private set; }

    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    private readonly List<Assignment> _assignments = new();
    public IReadOnlyCollection<Assignment> Assignments => _assignments.AsReadOnly();

    private Course() { }

    public Course(string title, string description, DateTime startsAt, DateTime endsAt)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title is required", nameof(title));
        }

        Title = title;
        Description = description;

        var startUtc = NormalizeToUtc(startsAt);
        var endUtc = NormalizeToUtc(endsAt);

        Period = TimePeriod.Create(startUtc, endUtc);
    }

    public TimePeriod Period { get; private set; }

    private static DateTime NormalizeToUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc),
            _ => value
        };
    }

    public void AddAssignment(Assignment assignment)
    {
        _assignments.Add(assignment);
    }
}
