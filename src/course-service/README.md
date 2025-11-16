# Team B — Course Service (Clean Architecture)

CQRS + MediatR ядро TrainingHub. Управляет курсами, заданиями, уведомлениями и прогнозом просрочек.

## Layers
```
CourseService/
  src/
    Application/     # CQRS, DTOs, validators
    Domain/          # Entities, ValueObjects, Aggregates
    Infrastructure/  # EF Core, Redis, RabbitMQ, ML.NET
    Presentation/    # Minimal API (опционально) / gRPC
  tests/
    UnitTests/
    IntegrationTests/
```

## Tech stack
- .NET 10, Clean Architecture
- MediatR, FluentValidation
- EF Core + PostgreSQL
- Redis cache
- RabbitMQ publisher/consumer
- Quartz hosted service (DailyReminderJob)
- ML.NET worker (predict overdue probability)
- Serilog logging + Seq sink
- xUnit + resp. test projects

## Состояние
1. Проекты Domain/Application/Infrastructure/Presentation + Worker.ML + Worker.Quartz собраны в составе каталога `src/course-service/src`.
2. EF Core DbContext (`CourseDbContext`) + миграции настроены под PostgreSQL.
3. CQRS handlers реализованы: `CreateCourse`, `GetCourseDetail`, `GetPopularCourses`, `ScheduleAssignment`, `GetAssignmentTrainingData`.
4. Redis cache (`RedisCourseCache`) ускоряет популярные курсы.
5. RabbitMQ publisher/consumer (`AssignmentScheduledEvent`) отправляет нотификации.
6. ML.NET worker (`Worker.ML/Worker.cs`) тренирует SDCA модель и сохраняет `assignment-model.zip`.
7. Quartz job (`DailyReminderJob`) выполняет ежедневные напоминания.
8. Unit/Integration tests (xUnit) покрывают домен/инфраструктуру.

## Commands
```bash
cd src/course-service
# (после генерации проектов)
dotnet test
```

## Git flow
- Ветки `feature/course-service/<feature>`.
- PR ревьюят API + Ops участники (см. `INSTRUCTION.md`, раздел 16).
