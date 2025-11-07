using Microsoft.EntityFrameworkCore;
using TrainingHub.CourseService.Application.Courses;
using TrainingHub.CourseService.Domain.Entities;

namespace TrainingHub.CourseService.Infrastructure.Persistence;

public class CourseRepository : ICourseRepository
{
    private readonly CourseDbContext _dbContext;

    public CourseRepository(CourseDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Course course, CancellationToken cancellationToken)
    {
        await _dbContext.Courses.AddAsync(course, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Course?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _dbContext.Courses
            .Include(c => c.Assignments)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyCollection<Course>> GetPopularAsync(int take, CancellationToken cancellationToken)
    {
        return await _dbContext.Courses
            .OrderByDescending(c => c.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Assignment>> GetAssignmentsAsync(Guid courseId, CancellationToken cancellationToken)
    {
        return await _dbContext.Assignments
            .Where(a => a.CourseId == courseId)
            .OrderBy(a => a.DueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<AssignmentTrainingData>> GetAssignmentTrainingDataAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Assignments
            .Select(a => new AssignmentTrainingData(
                a.CourseId,
                a.Id,
                a.DueDate,
                a.Status == AssignmentStatus.Completed))
            .ToListAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
