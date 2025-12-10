# Team C — Blazor Client

WebAssembly SPA для слушателей и кураторов. Получает данные через API gateway, слушает SignalR уведомления и поддерживает локализацию RU/EN.

## Стек
- .NET 10 / Blazor WebAssembly (standalone)
- HttpClient + Polly retry (через `CourseApiClient`)
- SignalR client (`NotificationHubClient`)
- Localization (`IStringLocalizer`, ресурсы en/ru)
- Dark neon UI (кастомный `wwwroot/css/app.css`)
- bUnit + xUnit тесты (`TrainingHub.BlazorClient.Tests`)

## Структура
```
TrainingHub.BlazorClient/
  Program.cs               # DI, HttpClient, SignalR
  wwwroot/css/app.css      # общая тема
  Pages/
    Home.razor             # Dashboard + формы
    Courses.razor
    Assignments.razor
  Services/
    ICourseApiClient.cs
    CourseApiClient.cs
    INotificationHubClient.cs
    NotificationHubClient.cs
  Shared/CourseDtos.cs
  Resources/TrainingHub.BlazorClient.Pages.*.resx
Tests/
  TrainingHub.BlazorClient.Tests/
    Pages/HomePageTests.cs
    Pages/CoursesPageTests.cs
    Services/CourseApiClientTests.cs
    Services/NotificationHubClientTests.cs
```

## Состояние
1. Локализация (en/ru) подключена, строки в `Resources`.
2. SignalR клиент слушает `/hub/notifications`.
3. Страницы Home/Courses/Assignments полностью реализованы (формы, фильтры, live-уведомления).
4. Темизация и responsive layout в `wwwroot/css/app.css`.
5. bUnit/xUnit тесты покрывают компоненты и API-клиент.
6. DI через интерфейсы `ICourseApiClient`, `INotificationHubClient` для удобных моков.

## Запуск
```bash
cd src/blazor-client/TrainingHub.BlazorClient
dotnet run
# приложение доступно по адресу https://localhost:7170
```

## Git flow
- Работать в ветке `feature/blazor-client/<feature>`.
- PR ревьюят API + Ops участники (см. `INSTRUCTION.md`, раздел 16).
