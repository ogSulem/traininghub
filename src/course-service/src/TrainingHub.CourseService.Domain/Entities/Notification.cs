namespace TrainingHub.CourseService.Domain.Entities;

public class Notification
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid CourseId { get; private set; }

    public string Message { get; private set; }

    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public Notification(Guid courseId, string message)
    {
        CourseId = courseId;
        Message = !string.IsNullOrWhiteSpace(message)
            ? message
            : throw new ArgumentException("Message is required", nameof(message));
    }
}
