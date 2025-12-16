# TrainingHub — учебная платформа для демонстрации enterprise-стека

MVP демонстрирует полный перечень технологий из ТЗ на примере внутренней платформы управления курсами.

## Архитектура

- **API Gateway** (Minimal API + SignalR) проксирует REST-запросы к course-service и транслирует уведомления в Blazor.
- **Course Service** (Clean Architecture, CQRS/MediatR, EF Core/PostgreSQL, Redis, RabbitMQ, Quartz, ML.NET worker) хранит домен и выполняет аналитику.
- **Blazor WebAssembly** клиент отображает Dashboard/Courses/Assignments, подключается к SignalR и поддерживает локализацию.
- **Ops tooling** orchestration: Docker Compose, GitLab CI, Postman/newman автотесты.

## Модули

| Каталог | Роль | Технологии |
|---------|------|------------|
| `src/api-gateway` | Edge/API + SignalR | ASP.NET Core Minimal API, REST, Swagger, SignalR, Serilog, Localization, Postman, DI/SOLID |
| `src/course-service` | ядро с Clean Architecture | Clean Architecture, CQRS + MediatR, EF Core + PostgreSQL, Redis Cache, RabbitMQ, Quartz jobs, ML.NET прогноз, тесты |
| `src/blazor-client` | клиентское SPA | Blazor WASM, SignalR client, локализация, unit/bUnit tests |
| `src/ops-tooling` | DevOps tooling | Docker/Compose, GitLab CI, newman tests, infra scripts |

## Требования → реализация

1. **Основы ООП** — доменные сущности `Course`, `Assignment`, шаблоны уведомлений.
2. **Clean Architecture / DI / SOLID** — в `course-service`.
3. **RESTful API** — `api-gateway` + Swagger + Postman.
4. **CQRS + MediatR** — команды/запросы в `course-service`.
5. **SignalR** — live-уведомления об изменениях курса (gateway ↔ client).
6. **Kafka/RabbitMQ** — RabbitMQ события `AssignmentScheduledEvent` + consumer.
7. **Quartz.NET** — планировщик напоминаний в отдельном воркере.
8. **Docker** — compose-файл в `ops-tooling`.
9. **Redis** — кэш активных курсов/прогнозов.
10. **Blazor** — SPA портал слушателя/куратора.
11. **Тестирование (unit/auto)** — xUnit, MSTest, bUnit, newman.
12. **БД / JSON / Swagger / Postman** — PostgreSQL, EF migrations, swagger.json, коллекция.
13. **Git & GitLab** — описанный workflow с ветками + CI.
14. **Logging** — Serilog + структурированные логи.
15. **Локализация** — ресурсы en/ru.
16. **ML/SL** — ML.NET worker прогнозирует вероятность просрочек.
17. **Git‑командность** — 4 папки = 4 команды, workflow описан ниже.

## Технологии и файлы

| № | Технология | Где смотреть |
|---|------------|--------------|
|1|ООП|`src/course-service/src/TrainingHub.CourseService.Domain/Entities/{Course,Assignment,Notification}.cs`|
|2|Clean Architecture|`src/course-service/src` (проекты): `TrainingHub.CourseService.Domain.csproj`, `TrainingHub.CourseService.Application.csproj`, `TrainingHub.CourseService.Infrastructure.csproj`, `TrainingHub.CourseService.Presentation.csproj`|
|3|RESTful API|`src/api-gateway/TrainingHub.ApiGateway/Program.cs` (Minimal API + Swagger)|
|4|DI / SOLID|`Program.cs` в API Gateway и `DependencyInjection.cs` в course-service|
|5|CQRS + MediatR|`src/course-service/.../Application/Courses/*Command*.cs`, `*Query*.cs`|
|6|Git workflow|Секция «Workflow и CI/CD» ниже (ветки, ревью, release)|
|7|GitLab CI|`.gitlab-ci.yml` в корне (stages: build/test/publish)|
|8|Docker|`src/ops-tooling/docker-compose.yml` (Postgres, Redis, RabbitMQ, services, Blazor)|
|9|SignalR|`src/api-gateway/.../Program.cs` (hub + internal endpoint), `src/blazor-client/.../NotificationHubClient.cs`|
|10|RabbitMQ|`src/course-service/.../Infrastructure/Messaging` (publisher/consumer)|
|11|Quartz|`src/course-service/.../Worker.Quartz/Jobs/DailyReminderJob.cs`|
|12|Redis|`src/course-service/.../Infrastructure/Caching/RedisCourseCache.cs`|
|13|Blazor|`src/blazor-client/TrainingHub.BlazorClient/Pages/*.razor`, `wwwroot/css/app.css`|
|14|Тестирование|`src/blazor-client/TrainingHub.BlazorClient.Tests/*`, `src/api-gateway/TrainingHub.ApiGateway.Tests`|
|15|БД / JSON / Swagger|`src/course-service/.../Persistence`, Swagger в API Gateway (`/swagger/index.html`)| 
|16|Postman/newman|`src/ops-tooling/postman/TrainingHub.postman_collection.json` + `newman run ...`|
|17|Логирование|`appsettings*.json` в API Gateway и course-service (Serilog + Seq)|
|18|Локализация|`src/api-gateway/.../Program.cs` (Accept-Language), `src/blazor-client/.../Resources/*.resx`|
|19|ML.NET|`src/course-service/.../Worker.ML/Worker.cs` (SDCA модель `assignment-model.zip`)|

## Команды (4 студента)

| Участник | Зона ответственности | Каталоги |
|----------|----------------------|----------|
|**Алексей (API)**|Minimal API, SignalR, локализация, Serilog, Swagger, Postman|`src/api-gateway`|
|**Борис (Core)**|CQRS/MediatR, PostgreSQL/EF Core, Redis, RabbitMQ, Quartz, ML.NET|`src/course-service`|
|**Кира (Client)**|Blazor UI, тёмная тема, локализация, SignalR клиент, bUnit/xUnit|`src/blazor-client`|
|**Денис (Ops)**|Docker Compose, GitLab CI, Postman/newman автотесты, README/инструкции|`src/ops-tooling`|

## Quick start

```bash
# 1. Сгенерировать решения (см. README внутри модулей)
# 2. Запустить docker-compose
cd src/ops-tooling
docker compose up --build

# 3. (опционально) запустить Blazor отдельно
cd ../blazor-client/TrainingHub.BlazorClient
dotnet run
```

### Тесты и линтеры

```bash
dotnet test src/api-gateway/TrainingHub.ApiGateway.Tests/TrainingHub.ApiGateway.Tests.csproj
dotnet test src/blazor-client/TrainingHub.BlazorClient.Tests/TrainingHub.BlazorClient.Tests.csproj
newman run src/ops-tooling/postman/TrainingHub.postman_collection.json
```

## Workflow и CI/CD

- **Branching**: `main` (release), `develop` (integration), feature-ветки `feature/api-gateway/*`, `feature/course-service/*`, `feature/blazor-client/*`, `feature/ops/*`.
- **PR правила**: Conventional Commits, ревью между командами (API ↔ Core ↔ Client ↔ Ops).
- **Release**: после merge в `develop` создаём `release/v1.0`, затем merge в `main` + тег `v1.0`.
- **GitLab CI (`.gitlab-ci.yml`)**:
  - `build`: build отдельных проектов (gateway/course-service/blazor)
  - `tests`: xUnit/bUnit тесты
  - `postman-newman`: manual job (Newman) — не ломает пайплайн, но доказывает Postman/Newman
  - `publish-*`: `dotnet publish` в каталоги `publish/*`
- **Docker Compose**: `src/ops-tooling/docker-compose.yml` поднимает Postgres, Redis, RabbitMQ, course-service, API Gateway, Blazor и ML worker.
- **Postman/newman**: коллекция лежит в `src/ops-tooling/postman`, используется локально и в CI.

## Демонстрация

1. Создайте курс через Swagger (`api-gateway`).
2. Убедитесь, что Blazor получает уведомление через SignalR.
3. Покажите RabbitMQ и Quartz логи (в course-service).
4. Запустите ML worker (`course-service/src/...Worker.ML`) и покажите сохранённую модель.
5. Откройте `INSTRUCTION.md` (раздел 15) и пройдитесь по каждому пункту.

## Документация

- `INSTRUCTION.md` — единственный канонический документ для защиты: сценарий демо, архитектура, роли 4 участников и разжёванное объяснение 19 технологий.
- Postman коллекция + GitLab CI файл находятся в `src/ops-tooling`.
