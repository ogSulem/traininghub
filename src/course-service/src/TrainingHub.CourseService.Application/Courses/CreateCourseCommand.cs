using MediatR;
using FluentValidation;
using TrainingHub.CourseService.Domain.Entities;
using TrainingHub.CourseService.Domain.ValueObjects;

namespace TrainingHub.CourseService.Application.Courses;

public record CreateCourseCommand(string Title, string Description, DateTime StartsAt, DateTime EndsAt)
    : IRequest<Guid>;

public class CreateCourseCommandValidator : AbstractValidator<CreateCourseCommand>
{
    public CreateCourseCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MinimumLength(3);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.StartsAt).LessThan(x => x.EndsAt);
    }
}

public class CreateCourseCommandHandler : IRequestHandler<CreateCourseCommand, Guid>
{
    private readonly ICourseRepository _repository;

    public CreateCourseCommandHandler(ICourseRepository repository)
    {
        _repository = repository;
    }

    public async Task<Guid> Handle(CreateCourseCommand request, CancellationToken cancellationToken)
    {
        var course = new Course(request.Title, request.Description, request.StartsAt, request.EndsAt);

        await _repository.AddAsync(course, cancellationToken);

        return course.Id;
    }
}

public interface ICourseRepository
{
    Task AddAsync(Course course, CancellationToken cancellationToken);
    Task<Course?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Course>> GetPopularAsync(int take, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Assignment>> GetAssignmentsAsync(Guid courseId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<AssignmentTrainingData>> GetAssignmentTrainingDataAsync(CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
