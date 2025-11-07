using MediatR;
using Microsoft.Extensions.Logging;
using TrainingHub.CourseService.Application.Courses;
using TrainingHub.CourseService.Domain.Entities;

namespace TrainingHub.CourseService.Application.Assignments;

public record ScheduleAssignmentCommand(Guid CourseId, string Title, DateTime DueDate, string Description) : IRequest<Guid>;

public class ScheduleAssignmentCommandHandler : IRequestHandler<ScheduleAssignmentCommand, Guid>
{
    private readonly ICourseRepository _courseRepository;
    private readonly IAssignmentEventPublisher _eventPublisher;
    private readonly ILogger<ScheduleAssignmentCommandHandler> _logger;

    public ScheduleAssignmentCommandHandler(ICourseRepository courseRepository, IAssignmentEventPublisher eventPublisher, ILogger<ScheduleAssignmentCommandHandler> logger)
    {
        _courseRepository = courseRepository;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task<Guid> Handle(ScheduleAssignmentCommand request, CancellationToken cancellationToken)
    {
        var course = await _courseRepository.GetByIdAsync(request.CourseId, cancellationToken)
            ?? throw new InvalidOperationException($"Course {request.CourseId} not found");

        var assignment = new Assignment(request.CourseId, request.Title, request.DueDate, request.Description);
        course.AddAssignment(assignment);

        await _courseRepository.SaveChangesAsync(cancellationToken);
        await _eventPublisher.PublishAssignmentScheduledAsync(new AssignmentScheduledEvent(
            assignment.Id,
            assignment.CourseId,
            assignment.Title,
            assignment.DueDate,
            DateTime.UtcNow), cancellationToken);

        return assignment.Id;
    }
}
