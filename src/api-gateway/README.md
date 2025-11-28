# Team A — API Gateway

ASP.NET Core Minimal API + SignalR. Экспонирует REST endpoints для курсов, трансляцию уведомлений и заглушки аутентификации.

## Стек
- .NET 10 / ASP.NET Core Web API
- Minimal API, Swagger, SignalR
- Serilog logging
- Localization middleware (ru/en)
- DI контейнер, интерфейсы `ICourseService`, `INotificationHub`
- Unit tests (xUnit)
- Postman коллекция (`../ops-tooling/postman/TrainingHub.postman_collection.json`)

## Быстрый старт
```bash
cd src/api-gateway/TrainingHub.ApiGateway
# локальный запуск
dotnet run

# swagger будет доступен по адресу https://localhost:7079/swagger
```

## Состояние
- Minimal API endpoints реализованы в `TrainingHub.ApiGateway/Program.cs`.
- HttpClient → course-service с Polly retry (`CourseClient`).
- SignalR Hub `/hub/notifications` и внутренний endpoint `/internal/notifications`.
- Локализация (резурсы en/ru) + `IStringLocalizer`.
- Serilog (консоль + Seq), дополнительные метрики можно включить через Docker.
- Postman коллекция лежит в `src/ops-tooling/postman/`.

## Tests
```bash
dotnet test
```

## Git flow
- Работать в ветке `feature/api-gateway/<feature>`.
- PR → ревью core/ops участников (см. `INSTRUCTION.md`, раздел 16).
