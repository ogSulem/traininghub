namespace TrainingHub.CourseService.Domain.ValueObjects;

public record TimePeriod(DateTime Start, DateTime End)
{
    public bool Contains(DateTime point) => point >= Start && point <= End;

    public static TimePeriod Create(DateTime start, DateTime end)
    {
        if (end <= start)
        {
            throw new ArgumentException("End must be greater than Start");
        }

        return new TimePeriod(start, end);
    }
}
