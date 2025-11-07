namespace TrainingHub.CourseService.Domain.Entities;

public class Assignment
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid CourseId { get; private set; }

    public string Title { get; private set; }

    public DateTime DueDate { get; private set; }
    public string Description { get; private set; }

    public AssignmentStatus Status { get; private set; } = AssignmentStatus.Planned;

    private Assignment() { }

    public Assignment(Guid courseId, string title, DateTime dueDate, string description)
    {
        CourseId = courseId;
        Title = !string.IsNullOrWhiteSpace(title) ? title : throw new ArgumentException("Title is required", nameof(title));
        DueDate = NormalizeToUtc(dueDate);
        Description = description;
    }

    public void MarkCompleted() => Status = AssignmentStatus.Completed;
    public void MarkOverdue() => Status = AssignmentStatus.Overdue;

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
}

public enum AssignmentStatus
{
    Planned = 0,
    Completed = 1,
    Overdue = 2
}
