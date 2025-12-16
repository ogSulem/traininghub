# TrainingHub — инструкция для презентации

Цель: показать полный стек из 19 технологий на одном учебном проекте. Ниже — краткий гайд, чтобы быстро объяснить идею преподу и команде.

## 1. История и сценарий
- **TrainingHub** — внутренняя платформа для управления курсами: расписание, задания, напоминания, прогноз успеваемости.
- Проект разбит на 4 модуля/"команды" (папки), чтобы имитировать работу нескольких групп.
- Каждый модуль демонстрирует свой набор технологий, но вместе они образуют единый продукт.
- **Роли**: в концепции есть два типа пользователей — **Преподаватель/Куратор** (создаёт и настраивает курсы и задания) и **Обучающийся/Студент** (просматривает свои курсы, видит ближайшие дедлайны, получает уведомления).
- **Текущий UI**: в демо мы заходим без авторизации, поэтому один и тот же интерфейс умеет и управлять курсами (как преподаватель), и просматривать их (как студент). На защите можно говорить, что это «кабинет пользователя платформы», который совмещает оба сценария.

## 2. Стек и соответствия требованиям
| № | Требование | Где реализовано |
|---|------------|------------------|
|1|ООП|Domain модели `Course`, `Assignment` и доменная сущность `Notification` в `course-service`|
|2|Clean Architecture|`src/course-service` (слои Domain/Application/Infrastructure/Presentation)|
|3|RESTful API|`src/api-gateway` (Minimal API + Swagger)|
|4|DI/SOLID|ASP.NET Core встроенный DI во всех сервисах|
|5|CQRS + MediatR|`course-service/Application`|
|6|Git|Общий репозиторий, ветки `feature/team-x/...`|
|7|GitLab / GitHub Actions|GitLab CI: `.gitlab-ci.yml` в корне репозитория. GitHub Actions CI: `.github/workflows/ci.yml` (build+tests) — если репо на GitHub.|
|8|Docker|`src/ops-tooling/docker-compose.yml`|
|9|SignalR|`api-gateway` ↔ `blazor-client`|
|10|Kafka/RabbitMQ|`course-service` (RabbitMQ события `AssignmentScheduledEvent` + consumer)|
|11|Quartz|Отдельный воркер `src/course-service/src/TrainingHub.CourseService.Worker.Quartz` (`DailyReminderJob`)|
|12|Redis|Кэш популярных курсов в `course-service`|
|13|Blazor|`src/blazor-client`|
|14|Тестирование|xUnit / bUnit / Postman (newman) |
|15|БД / JSON / Swagger|PostgreSQL (EF Core), swagger.json, REST структуру|
|16|Postman|Коллекция в `src/ops-tooling/postman/TrainingHub.postman_collection.json`|
|17|Логирование|Serilog + Seq sink|
|18|Локализация|`api-gateway` middleware + `blazor-client` ресурсы|
|19|ML/SL|ML.NET worker (прогноз просрочек)|

## 3. Как показать работу команд
1. **Структура**: `src/api-gateway`, `src/course-service`, `src/blazor-client`, `src/ops-tooling`.
2. **README в каждой папке** описывает задачи модуля и стек.
3. **Workflow**: см. раздел **16** этого файла (ветки, PR/ревью, релиз).
4. **Доказательства технологий**: см. раздел **15** этого файла (пути к файлам и как показать).

## 3.1. Карта репозитория «по папкам и ролям» (что обязательно знать на защите)

Цель этого раздела: если тебя остановят вопросом “а что в этой папке и почему?”, ты отвечаешь уверенно и показываешь 1–2 ключевых файла.

### 3.1.1. `src/api-gateway/` — команда Gateway

- **Роль модуля:** единая точка входа для UI и внешних клиентов.
- **Что делает:**
  - проксирует REST-запросы в `course-service`;
  - принимает внутренние уведомления от `course-service` (internal endpoint);
  - рассылает уведомления в браузер через SignalR;
  - включает Swagger, логирование, локализацию, Polly retry.
- **Что обязательно показать в коде:**
  - `src/api-gateway/TrainingHub.ApiGateway/Program.cs` (Swagger, SignalR, internal endpoint, HttpClient + Polly, Serilog, Localization).
- **Что обязательно показать в рантайме:**
  - Swagger: `http://localhost:5000/swagger/index.html`.
  - SignalR: UI получает уведомление без обновления страницы.

### 3.1.2. `src/course-service/` — команда Core/Backend

- **Роль модуля:** “мозг” системы: доменная модель + хранение данных + интеграции инфраструктуры.
- **Что делает:**
  - хранит курсы/задания в PostgreSQL (EF Core);
  - реализует CQRS (MediatR) и валидацию (FluentValidation);
  - кэширует чтение популярных курсов в Redis;
  - публикует события в RabbitMQ и потребляет их consumer'ом;
  - отправляет уведомления в gateway через `NotificationDispatcher`.
- **Что обязательно показать в коде:**
  - Domain: `src/course-service/src/TrainingHub.CourseService.Domain/Entities/*.cs`
  - Application: `...Application/Courses/*`, `...Application/Assignments/*`, `...Application/Behaviors/ValidationBehavior.cs`
  - Infrastructure: `...Infrastructure/DependencyInjection.cs`, `...Infrastructure/Messaging/*`, `...Infrastructure/Caching/*`, `...Infrastructure/Notifications/*`
  - Presentation: `...Presentation/Program.cs` (Minimal API endpoints, Swagger, demo endpoints, сидинг данных, ML predict endpoint).

### 3.1.3. `src/course-service/src/TrainingHub.CourseService.Worker.Quartz/` — команда Schedulers

- **Роль модуля:** плановые фоновые задачи.
- **Что делает:** по cron вызывает `course-service` и рассылает напоминания через `NotificationDispatcher`.
- **Что обязательно показать:**
  - `Worker.Quartz/Jobs/DailyReminderJob.cs`
  - `Worker.Quartz/Program.cs` (cron из конфигурации, запуск воркера)
  - В Docker cron переопределён на каждую минуту, чтобы на защите не ждать.

### 3.1.4. `src/course-service/src/TrainingHub.CourseService.Worker.ML/` — команда Analytics/ML

- **Роль модуля:** обучение ML модели по расписанию.
- **Что делает:**
  - забирает тренировочные данные из `course-service` (`GET /analytics/assignments/training`);
  - обучает модель ML.NET;
  - сохраняет модель в docker volume `ml_models` (`/app/models/assignment-model.zip`).
- **Что важно проговорить на защите:**
  - обучение и использование модели разведены: обучение — worker, использование — API endpoint (`POST /analytics/assignments/predict`).

### 3.1.5. `src/blazor-client/` — команда Frontend

- **Роль модуля:** UI в браузере на C# (Blazor WASM).
- **Что делает:**
  - показывает курсы/задания;
  - ходит в Gateway по REST;
  - подписывается на SignalR уведомления;
  - переключает RU/EN через ресурсы `.resx` и сохранение культуры.
- **Что обязательно показать:**
  - `Pages/*.razor` (особенно `Assignments.razor` — баннер уведомлений)
  - `Services/NotificationHubClient.cs` (подключение к hub)
  - `Layout/NavMenu.razor` + `wwwroot/index.html` (локализация)

### 3.1.6. `src/ops-tooling/` — команда Ops/Infra

- **Роль модуля:** стенд “как мини-прод” и инструменты демонстрации.
- **Что делает:**
  - поднимает инфраструктуру (Postgres/Redis/RabbitMQ/Seq) и сервисы одной командой через Docker Compose;
  - хранит Postman-коллекцию для ручного и автоматического API smoke.
- **Что обязательно показать:**
  - `src/ops-tooling/docker-compose.yml`
  - `src/ops-tooling/postman/TrainingHub.postman_collection.json`

### 3.1.7. Корень репозитория (CI/CD)

- **GitLab CI/CD:** `.gitlab-ci.yml`
- **GitHub Actions (если репо в GitHub):** `.github/workflows/ci.yml`

Важно: CI/CD показываем “честно”: что реально есть в репозитории и реально запускается на вашей платформе (GitLab или GitHub).

## 4. Как запускать (MVP)
1. Установить .NET 10 SDK, Docker Desktop, Node (для Blazor build) — указать при защите.
2. В корне `traininghub`:
   ```bash
   cd src/api-gateway/TrainingHub.ApiGateway
   dotnet run
   # служба доступна по https://localhost:7079/swagger
   ```
3. После добавления остальных сервисов:
   ```bash
   cd src/ops-tooling
   docker compose up --build
   ```

## 5. Путь развития (что ещё можно добавить)
- Подключить Prometheus/Grafana в docker-compose.
- Playwright e2e для Blazor.
- Расширить ML-пайплайн новыми фичами и датасетами.
- Автоматизировать newman в CI (отдельный job).

## 6. Как объяснить каждую технологию

| № | Технология | Что сказать / показать (код + рантайм) |
|---|------------|----------------------------------------|
|1|**ООП**|Код: `src/course-service/src/TrainingHub.CourseService.Domain/Entities/{Course,Assignment}.cs`. Показать, что это сущности с поведением/инвариантами.|
|2|**Clean Architecture**|Код: `src/course-service/src` (отдельные проекты): `TrainingHub.CourseService.Domain.csproj`, `TrainingHub.CourseService.Application.csproj`, `TrainingHub.CourseService.Infrastructure.csproj`, `TrainingHub.CourseService.Presentation.csproj`. Объяснить направление зависимостей внутрь.|
|3|**RESTful API (Minimal API)**|Код: `src/api-gateway/TrainingHub.ApiGateway/Program.cs`, `src/course-service/src/TrainingHub.CourseService.Presentation/Program.cs`. Рантайм: Swagger `http://localhost:5000/swagger/index.html`, `http://localhost:8080/swagger/index.html`.|
|4|**DI / SOLID**|Код: `src/course-service/src/TrainingHub.CourseService.Infrastructure/DependencyInjection.cs` (регистрации DbContext/Redis/RabbitMQ/NotificationDispatcher), `src/api-gateway/TrainingHub.ApiGateway/Program.cs` (HttpClient/Polly/SignalR).|
|5|**CQRS + MediatR (+ FluentValidation)**|Код: `src/course-service/src/TrainingHub.CourseService.Application/*`, `.../Behaviors/ValidationBehavior.cs`, пример `.../Courses/CreateCourseCommand.cs`. Рантайм: в Swagger показать 400 `ValidationProblem` при плохих данных.|
|6|**Git**|Показать историю/ветки `feature/*` и что работа велась через PR/MR (см. раздел 16).|
|7|**CI/CD (GitLab CI или GitHub Actions)**|GitLab CI: `.gitlab-ci.yml`. GitHub Actions CI: `.github/workflows/ci.yml`. Что показать: build/test и TRX артефакты тестов.|
|8|**Docker Compose**|Код: `src/ops-tooling/docker-compose.yml`. Рантайм: `docker compose ps` → сервисы `healthy`, открываются URL из раздела 11.|
|9|**SignalR**|Код: Gateway `src/api-gateway/TrainingHub.ApiGateway/Program.cs` (`MapHub("/hub/notifications")` + `POST /internal/notifications/assignments`), Blazor `src/blazor-client/TrainingHub.BlazorClient/Services/NotificationHubClient.cs`. Рантайм: создать задание → событие `AssignmentScheduled` прилетает в UI.|
|10|**RabbitMQ**|Код: publisher `src/course-service/src/TrainingHub.CourseService.Infrastructure/Messaging/RabbitMqAssignmentEventPublisher.cs`, consumer `.../RabbitMqAssignmentEventConsumer.cs`. Рантайм: `GET http://localhost:8080/demo/rabbitmq` + Seq: `Published...` → `Received...` → `Broadcasting...`.|
|11|**Quartz.NET**|Код: `src/course-service/src/TrainingHub.CourseService.Worker.Quartz/Program.cs`, job `.../Jobs/DailyReminderJob.cs`. Рантайм: в Docker cron ускорен (`Quartz__DailyReminderCron`) → в Seq видно периодическое выполнение.|
|12|**Redis**|Код: `src/course-service/src/TrainingHub.CourseService.Infrastructure/Caching/RedisCourseCache.cs`. Рантайм: `GET http://localhost:8080/demo/redis` и в Seq показать `Redis cache miss` → `Redis cache set` → `Redis cache hit`.|
|13|**Blazor WASM**|Код: `src/blazor-client/TrainingHub.BlazorClient/Pages/*.razor`, `Program.cs`. Рантайм: UI `http://localhost:5173`.|
|14|**Тестирование**|Код: `src/api-gateway/TrainingHub.ApiGateway.Tests/UnitTest1.cs` (smoke/integration через `WebApplicationFactory`), `src/blazor-client/TrainingHub.BlazorClient.Tests/*` (bUnit/xUnit). Рантайм: `dotnet test` + артефакты TRX в CI.|
|15|**PostgreSQL + EF Core (+ JSON/Swagger)**|Код: `src/course-service/src/TrainingHub.CourseService.Infrastructure/Persistence/CourseDbContext.cs`, инициализация `EnsureCreated()` в `...Presentation/Program.cs`. Рантайм: контейнер Postgres в compose + Swagger отдаёт JSON схемы.|
|16|**Postman/Newman**|Код: `src/ops-tooling/postman/TrainingHub.postman_collection.json`. Рантайм: запуск newman локально или job `postman-newman` в GitLab.|
|17|**Логирование (Serilog + Seq)**|Код: Serilog конфиг в `appsettings.json` сервисов + `UseSerilog(...Enrich.WithProperty("Application", ...))`. Рантайм: `http://localhost:8081` (Seq UI), фильтры по `Application`.|
|18|**Локализация**|Код: Gateway `Program.cs` (`UseRequestLocalization`), Blazor `Program.cs` + `.resx` в `src/blazor-client/TrainingHub.BlazorClient/Resources/*` + переключатель в меню. Рантайм: RU/EN переключение.|
|19|**ML.NET (обучение + предсказание)**|Обучение: `src/course-service/src/TrainingHub.CourseService.Worker.ML/Worker.cs` сохраняет модель в `ml_models`. Предсказание: `POST http://localhost:8080/analytics/assignments/predict` читает модель и возвращает `probability/score`.|

## 7. Что говорить на защите
- **Почему 4 каталога?** → четыре направления разработки, каждая папка = отдельный scope и технология.
- **Как все связано?** → Gateway ←→ Course Service через HTTP/RabbitMQ, Blazor через SignalR, Ops orchestrates Docker/CI.
- **Где доказательства каждой технологии?** → Ссылка на таблицу выше; открываем нужный файл/лог/скрин.
- **Демо**:
  1. Запускаем `api-gateway` → Swagger.
  2. Создаём курс → SignalR уведомление в Blazor.
  3. Показываем лог RabbitMQ и Quartz job.
  4. Выводим Redis cache hit в логах.
  5. Показываем Seq с логами и ML прогноз.

Документ обновляем по мере прогресса (добавляем ссылки на конкретные классы/файлы).

## 8. Термины «на человеческом языке»

- **Endpoint (эндпоинт)** — конкретный адрес HTTP‑запроса, например `GET /courses` или `POST /courses/{id}/assignments`.
- **Minimal API** — стиль написания API в ASP.NET Core, когда вместо контроллеров используются короткие вызовы `MapGet`, `MapPost` прямо в `Program.cs`.
- **MediatR** — библиотека‑посредник: контроллер или endpoint не выполняет бизнес‑логику сам, а отправляет «команду» или «запрос» в MediatR, и уже отдельный handler делает работу.
- **Handler** — класс, который обрабатывает одну конкретную команду или запрос (например, `CreateAssignmentCommandHandler`).
- **Pipeline (пайплайн)** — цепочка шагов/сервисов, через которые проходит одно действие пользователя.
- **RabbitMQ** — брокер сообщений. Можно представить как «почтовый офис», где сервисы обмениваются письмами (сообщениями) не напрямую, а через очереди.
- **Exchange** — «точка входа» в RabbitMQ. Сервисы публикуют сообщения в exchange, а он уже раскладывает их по очередям.
- **Routing key** — строковый «маркер тематики» сообщения (например, `assignments.scheduled.created`), по которому RabbitMQ решает, в какие очереди положить сообщение.
- **Queue (очередь)** — хранилище сообщений, из которого читает потребитель (consumer). У нас, например, очередь `traininghub.notifications`.
- **SignalR** — библиотека для постоянного соединения между сервером и браузером. Позволяет серверу «толкать» события в реальном времени всем подключенным клиентам.

## 9. Полный путь: создаём задание с дедлайном и проходим через все технологии

Этот сценарий можно почти дословно рассказывать на защите. Он показывает работу почти всего стека сразу.

### 9.1. Кто что делает

- **Преподаватель** заходит в веб‑интерфейс Blazor.
- Открывает страницу курсов, выбирает курс и нажимает «Создать задание».
- Заполняет название задания и дедлайн и нажимает «Сохранить».

### 9.2. Шаг 1 — Blazor WebAssembly (клиент)

1. В браузере работает Blazor WebAssembly‑клиент.
2. Нажатие кнопки «Сохранить задание» вызывает метод в C#‑коде страницы, который обращается к сервису `ICourseApiClient`.
3. `ICourseApiClient` использует `HttpClient` и отправляет HTTP‑запрос `POST` на **API Gateway** на эндпоинт вида:
   - `POST /courses/{courseId}/assignments`
4. В теле запроса отправляются данные задания: заголовок, описание, дедлайн и т.п. (JSON).

Здесь уже можно подчеркнуть:

- Используются **RESTful API** (чёткие HTTP‑эндпоинты, методы GET/POST, JSON).
- В Blazor мы не пишем JavaScript, всё на C#, а в браузере крутится WebAssembly.

### 9.3. Шаг 2 — API Gateway и Minimal API

5. Запрос попадает в **API Gateway**.
6. В `Program.cs` API Gateway определён Minimal API‑эндпоинт, который принимает этот запрос, валидирует данные и перенаправляет его во внутренний **Course Service** (либо напрямую вызывает его HTTP‑эндпоинт, либо через зарегистрированный HttpClient/клиента).
7. Здесь можно показать в Swagger, как выглядит этот эндпоинт.

Ключевая идея: **Gateway — единая точка входа** для всех клиентов.

### 9.4. Шаг 3 — Course Service, MediatR и EF Core

8. Внутри `CourseService.Presentation` есть Minimal API‑эндпоинт, который принимает запрос от Gateway.
9. Этот эндпоинт не делает бизнес‑логику сам, а создаёт команду `CreateAssignmentCommand` и отправляет её через **MediatR**.
10. MediatR находит соответствующий **handler** (например, `CreateAssignmentCommandHandler`).
11. В handler’е выполняется бизнес‑логика:
    - проверка входных данных (через FluentValidation или вручную);
    - создание сущности `Assignment` в доменной модели;
    - сохранение задания в PostgreSQL через **EF Core** (контекст БД).
12. После успешного сохранения handler возвращает результат (Id задания, данные и т.п.) назад по цепочке (в Minimal API → в Gateway → в Blazor‑клиент).

На этом шаге можно сказать:

- «Здесь мы демонстрируем **Clean Architecture + CQRS + MediatR + EF Core + PostgreSQL**».

### 9.5. Шаг 4 — Публикация события в RabbitMQ

13. В том же handler’е (или сразу после него) формируется событие, например `AssignmentScheduledEvent`, где есть Id курса, Id задания, заголовок и дедлайн.
14. Это событие отправляется в специальный сервис `RabbitMqAssignmentEventPublisher`.
15. `RabbitMqAssignmentEventPublisher` устанавливает соединение с RabbitMQ и публикует сообщение в **exchange** `traininghub.assignments` с определённым **routing key**, например `assignments.scheduled.created`.

Важно проговорить:

- «Здесь мы отделяем **создание задания** от **отправки уведомлений** с помощью брокера сообщений RabbitMQ. Это делает систему более устойчивой: если уведомления временно недоступны, создание задания всё равно завершится.»

### 9.6. Шаг 5 — Consumer, очередь и NotificationDispatcher

16. В отдельном фоновой службе (в составе Course Service) работает **RabbitMqAssignmentEventConsumer`**.
17. При запуске он подключается к RabbitMQ, создаёт/подписывается на очередь `traininghub.notifications` и привязывает её к exchange по шаблону routing key `assignments.scheduled.*`.
18. Как только новое сообщение попадает в очередь, consumer его считывает, десериализует `AssignmentScheduledEvent` и вызывает сервис `NotificationDispatcher`.
19. `NotificationDispatcher` формирует HTTP‑запрос к **notification‑endpoint** в API Gateway, например `POST /notifications/assignments`, и передаёт туда информацию о задании и дедлайне.

Здесь показываем:

- код consumer’а (подписка на очередь);
- код `NotificationDispatcher` (HTTP‑вызов в Gateway).

### 9.7. Шаг 6 — API Gateway и SignalR Hub

20. API Gateway принимает HTTP‑запрос от NotificationDispatcher на эндпоинт `/notifications/assignments`.
21. Внутри этого эндпоинта вызывается **SignalR Hub**, зарегистрированный в Gateway.
22. Hub рассылает событие (например, метод `AssignmentScheduled`) всем подключённым клиентам или в конкретную группу пользователей.

Ключевая фраза для защиты:

- «Таким образом, Course Service даже не знает, сколько у нас подключено клиентов и кто они. Он просто шлёт уведомление в Gateway, а **SignalR** уже занимается realtime‑доставкой.»

### 9.8. Шаг 7 — Blazor‑клиент и NotificationHubClient

23. В Blazor‑клиенте есть сервис `NotificationHubClient`, который при старте приложения устанавливает соединение с SignalR‑хабом в Gateway.
24. Клиент подписывается на событие `AssignmentScheduled` (или аналогичное имя метода).
25. Когда от сервера приходит уведомление, `NotificationHubClient` вызывает зарегистрированный callback в странице Home/Courses.
26. Страница обновляет список «Ближайшие задания» и/или показывает всплывающее уведомление — **без перезагрузки страницы**.

Здесь можно сказать:

- «Так реализуется realtime‑обновление фронтенда через **SignalR**. Пользователь сразу видит новое задание или напоминание.»

### 9.9. Альтернативный путь — Quartz DailyReminderJob

Отдельно можно описать второй сценарий, который не зависит от того, что преподаватель только что создал задание:

27. В docker‑композе запущен отдельный Quartz‑воркер (`course-service-worker-quartz`).
28. В нём настроен job `DailyReminderJob`, который срабатывает по расписанию (например, каждый день в 18:00).
29. Job опрашивает Course Service (через HTTP) и ищет курсы/задания, по которым нужно отправить напоминания.
30. Для каждого такого задания Quartz‑job вызывает тот же `NotificationDispatcher`.
31. Дальше путь идентичен: API Gateway → SignalR Hub → Blazor NotificationHubClient → обновление UI.

Итого на этом сценарии ты демонстрируешь:

- Blazor WebAssembly (UI),
- Minimal API,
- MediatR + CQRS,
- EF Core + PostgreSQL,
- RabbitMQ (publisher + consumer),
- HTTP notification gateway,
- SignalR (real‑time уведомления),
- Quartz (ежедневные напоминания),
- а также общую микросервисную архитектуру и взаимодействие сервисов.


## 10. Архитектура (как объяснить за 30–60 секунд)

Скажи буквально так (и покажи схему ниже):

> «TrainingHub — это учебная мини‑платформа для курсов. Внутри есть клиент (Blazor), входная дверь (API Gateway), ядро данных/бизнес‑логики (Course Service) и инфраструктура (PostgreSQL/Redis/RabbitMQ + воркеры Quartz/ML). Всё поднимается через Docker Compose одной командой.»

Текстовая схема (её можно держать в голове):

```
 [Blazor Client]
   |  REST (HttpClient)
   |  SignalR (live)
   v
 [API Gateway]  ---- REST ---->  [Course Service]
     |                                | \
     | SignalR Hub                    |  \
     |                                |   \
     +-- internal /notifications <-----+    \
                                         PostgreSQL
                                         Redis
                                         RabbitMQ

 [Quartz Worker] ----HTTP----> [Course Service]
       |                         |
       +----HTTP notify----------+
 [ML.NET Worker] ----HTTP----> [Course Service]

 [ops-tooling] = docker-compose + CI + Postman
```

Ключевые мысли для вопросов преподавателя:

- **Зачем gateway?**
  - Единая точка входа для UI и внешних клиентов.
  - Тут же SignalR Hub (удобно: один адрес для браузера).
- **Зачем RabbitMQ?**
  - Создание задания и отправка уведомлений разделены.
  - Система устойчивее: уведомления могут временно падать, но создание задания не ломается.
- **Зачем Quartz?**
  - Плановые напоминания «каждый день в 18:00».


## 11. Как запускать (идеально для защиты)

### 11.1. Требования

- Docker Desktop (обязательно)
- .NET SDK 10 (желательно, но для docker‑запуска не всегда нужен)

### 11.2. Запуск одной командой

Команда выполняется из `src/ops-tooling`:

```bash
docker compose up --build
```

### 11.3. Что должно быть в итоге (адреса)

- **Blazor UI**: `http://localhost:5173`
- **API Gateway Swagger**: `http://localhost:5000/swagger/index.html`
- **Course Service Swagger**: `http://localhost:8080/swagger/index.html`
- **RabbitMQ Management**: `http://localhost:15672` (`guest`/`guest`)
- **Seq (логи, UI)**: `http://localhost:8081` (ingest: `http://localhost:5341`)

Если хотя бы 3 первых пункта открываются — демо готово.

### 11.3.1. Чек‑лист готовности стенда (30 секунд)

- **1) Контейнеры healthy**
  - `docker compose ps`
  - ожидаем: `postgres`, `redis`, `rabbitmq`, `seq`, `course-service`, `api-gateway` в состоянии `healthy`.
- **2) Health endpoints отвечают**
  - `http://localhost:8080/health` → `ok`
  - `http://localhost:5000/health` → `ok`
- **3) Основные UI/Swagger открываются**
  - `http://localhost:5173` (Blazor UI)
  - `http://localhost:5000/swagger/index.html` (Gateway)
  - `http://localhost:8080/swagger/index.html` (Course Service)
- **4) Логи доступны**
  - `http://localhost:8081` (Seq UI)

### 11.4. Если что-то не стартует

Быстрые команды (сначала это, потом раздел 17):

- **Посмотреть, кто не healthy**
  - `docker compose ps`
- **Посмотреть логи конкретного сервиса**
  - `docker compose logs -n 200 course-service`
  - `docker compose logs -n 200 api-gateway`
  - `docker compose logs -n 200 rabbitmq`
- **Проверить, что API реально отвечает**
  - `curl -fsS http://localhost:8080/health`
  - `curl -fsS http://localhost:5000/health`

Смотри раздел **17. Траблшутинг** в конце этого файла (там собраны типовые причины).


## 12. Роли 4 участников (кто что рассказывает и что открывает)

Это раздел можно распечатать и раздать участникам. У каждого — чёткий сценарий.

### 12.1. Участник A — API Gateway (Minimal API, Swagger, SignalR, Localization, Serilog)

**Что говорить (коротко):**

> «Gateway — единая точка входа. Он принимает REST‑запросы от Blazor/Postman, проксирует их в Course Service и рассылает уведомления в браузер через SignalR.»

**Что открыть в коде (минимальный набор):**

- `src/api-gateway/TrainingHub.ApiGateway/Program.cs`

**Что показать в `Program.cs` (по пунктам):**

1. `AddSwaggerGen` / `UseSwagger` — документация API.
2. `AddSignalR` + `MapHub(..."/hub/notifications")` — realtime.
3. `AddLocalization` + `UseRequestLocalization` — ru/en.
4. HttpClient к course-service + Polly retry — надёжные вызовы.
   - Polly настройки: `src/api-gateway/TrainingHub.ApiGateway/appsettings.json` (`Polly:RetryCount`, `Polly:RetryDelaySeconds`)
   - Polly включение: `src/api-gateway/TrainingHub.ApiGateway/Program.cs` (`AddHttpClient("CourseService")` + `WaitAndRetryAsync(...)`)
5. Serilog — структурные логи.

**Что показать вживую:**

- Открыть Swagger Gateway.
- Выполнить `GET /courses`.
- Выполнить `POST /courses`.
- Выполнить `POST /courses/{id}/assignments`.

**Как отвечать на вопрос «Почему клиент ходит в gateway, а не в сервис?»**

> «Чтобы у клиента был один адрес, единые правила CORS/локализации, единая документация и единый realtime‑hub. Внутренние сервисы можно менять без изменения клиента.»

### 12.2. Участник B — Course Service (Clean Architecture, MediatR, EF/Postgres, Redis, RabbitMQ)

**Что говорить:**

> «Course Service — мозг системы. Здесь доменная модель, правила, хранение в Postgres, кэш Redis, публикация событий RabbitMQ и фоновые воркеры.»

**Что открыть:**

- `src/course-service/src/TrainingHub.CourseService.Domain/*` (ООП)
- `src/course-service/src/TrainingHub.CourseService.Application/*` (CQRS/MediatR)
- `src/course-service/src/TrainingHub.CourseService.Infrastructure/DependencyInjection.cs` (DI + Redis + RabbitMQ)
- `src/course-service/src/TrainingHub.CourseService.Infrastructure/Messaging/RabbitMq*.cs` (publisher/consumer)

**Что показать вживую:**

- Swagger course-service: `GET /courses`.
- Логи контейнера course-service при создании задания (публикация события).

**Как отвечать «Зачем MediatR?»**

> «Endpoint остаётся тонким: он только принимает запрос и отправляет команду. Логика в handler’ах — её проще тестировать, расширять и не смешивать с HTTP.»

### 12.3. Участник C — Blazor Client (UI, REST client, SignalR client, Localization)

**Что говорить:**

> «Blazor WebAssembly — SPA в браузере на C#. Он получает курсы и задания через REST и слушает уведомления через SignalR.»

**Что открыть:**

- `src/blazor-client/TrainingHub.BlazorClient/Pages/Home.razor`
- `src/blazor-client/TrainingHub.BlazorClient/Pages/Courses.razor`
- `src/blazor-client/TrainingHub.BlazorClient/Services/CourseApiClient.cs`
- `src/blazor-client/TrainingHub.BlazorClient/Services/NotificationHubClient.cs`
- `src/blazor-client/TrainingHub.BlazorClient/Resources/*` (ru/en)

**Что показать вживую:**

- Home: популярные курсы и задания.
- Создать курс/задание.
- Показать, что уведомление/обновление пришло без перезагрузки.

### 12.4. Участник D — Ops (Docker Compose, GitLab CI, Postman, Git workflow)

**Что говорить:**

> «Ops‑часть позволяет развернуть всё одной командой, а CI прогоняет сборку и тесты автоматически. Postman/Newman — для автопроверки REST сценариев.»

**Что открыть:**

- `src/ops-tooling/docker-compose.yml`
- `.gitlab-ci.yml`
- `src/ops-tooling/postman/TrainingHub.postman_collection.json`
- (этот файл) раздел **16. Git workflow на 4 человека**


## 13. Сценарии демо (готовые «скрипты», чтобы не думать)

### 13.1. Демо на 5 минут (если времени мало)

1. Открыть Blazor UI (`http://localhost:5173`) → показать несколько курсов.
2. Открыть Gateway Swagger (`http://localhost:5000/swagger/index.html`) → показать эндпоинты.
3. Создать курс/задание (через UI или Swagger).
4. Сказать 2 фразы про pipeline уведомлений:
   - «Создали задание → событие в RabbitMQ → consumer → gateway → SignalR → UI обновился.»
5. Открыть раздел **15** этого файла и быстро пройтись: «вот файлы‑доказательства».

### 13.2. Демо на 15 минут (стандарт)

1. `docker compose up --build` (показать сервисы в compose).
2. Swagger gateway: `POST /courses`, `POST /courses/{id}/assignments`.
3. Blazor UI: показать появление курса/задания.
4. Показать consumer RabbitMQ в логах (публикация/получение) и объяснить почему это устойчиво.
5. Показать Quartz worker и cron «каждый день 18:00» (в коде).
6. В конце: «19 технологий — вот где каждая в коде».

### 13.3. Демо на 25–30 минут (если просят подробно)

Добавить:

- Redis: показать в коде, что популярные курсы кэшируются.
- ML.NET worker: открыть `Worker.ML` и объяснить «обучаем простую модель и сохраняем артефакт».
- CI: открыть `.gitlab-ci.yml` и объяснить стадии.

### 13.4. Демо без ограничений по времени (60–90 минут, “показать всё”)

Это сценарий “максимальная защита”. Идёшь сверху вниз и по пути закрываешь все 19 технологий.

1. **Старт (2 минуты)**
   - Открыть этот `INSTRUCTION.md`.
   - Сказать 2–3 предложения из раздела 10 (архитектура).
2. **Старт стенда (3–5 минут)**
   - Открыть `src/ops-tooling/docker-compose.yml` и показать состав сервисов.
   - Команда: `docker compose up --build`.
   - Показать `docker compose ps` → `healthy`.
3. **Swagger и REST (5–10 минут)**
   - Gateway Swagger: `http://localhost:5000/swagger/index.html`.
   - Course Service Swagger: `http://localhost:8080/swagger/index.html`.
   - Показать несколько эндпоинтов, объяснить Minimal API.
4. **Демо-данные и БД (5–10 минут)**
   - Открыть `src/course-service/src/TrainingHub.CourseService.Presentation/Program.cs` и показать сидинг demo-курсов.
   - Сказать: «Данные гарантированы при запуске, потому что сервис при старте создаёт несколько курсов и хотя бы одно задание. Для ML одно из заданий помечается Completed, чтобы были оба класса.»
5. **ООП + Domain (5 минут)**
   - Открыть `Course.cs`, `Assignment.cs`.
   - Объяснить инварианты и методы (не “таблички”, а сущности).
6. **Clean Architecture (5 минут)**
   - Показать 4 проекта Domain/Application/Infrastructure/Presentation.
   - Показать зависимость внутрь.
7. **CQRS/MediatR/FluentValidation (10 минут)**
   - Открыть `CreateCourseCommand.cs`, `ScheduleAssignmentCommand.cs`.
   - Показать `ValidationBehavior.cs`.
   - В Swagger отправить “плохой” запрос и получить 400 `ValidationProblem`.
8. **Redis (5 минут)**
   - Вызвать `GET /demo/redis`.
   - В Seq показать `miss → set → hit`.
9. **RabbitMQ + consumer (10 минут)**
   - Вызвать `GET /demo/rabbitmq`.
   - В Seq показать `Published` → `Received` → `Dispatched`.
   - В RabbitMQ UI показать очередь `traininghub.notifications`.
10. **SignalR end-to-end (10 минут)**
   - Открыть UI `http://localhost:5173/assignments`.
   - Повторно вызвать `GET /demo/rabbitmq`.
   - Показать баннер уведомления в UI.
11. **Quartz (5–10 минут)**
   - В docker-compose показать `Quartz__DailyReminderCron` (каждую минуту).
   - В Seq показать `Running DailyReminderJob ...` и далее отправку уведомлений.
12. **ML.NET (10–15 минут)**
   - В Seq показать `Saved ML model ...`.
   - В Swagger вызвать `POST /analytics/assignments/predict` и показать `probability/score`.
13. **Логирование (5 минут)**
   - Открыть Seq `http://localhost:8081`, фильтровать по `Application`.
   - Показать `DemoCorrelationId` и как удобно доказывать цепочку.
14. **Локализация (3–5 минут)**
   - В UI переключить RU/EN.
   - Открыть `.resx` и показать источник строк.
15. **Тестирование (5–10 минут)**
   - Открыть тесты xUnit/bUnit.
   - При необходимости прогнать `dotnet test` (или показать артефакты TRX в CI).
16. **Postman/Newman (5–10 минут)**
   - Открыть Postman коллекцию и показать, что переменные/ID прокидываются автоматически.
   - (Опционально) запустить newman.
17. **CI/CD (5–10 минут)**
   - GitLab: `.gitlab-ci.yml` — стадии build/test/publish.
   - GitHub: `.github/workflows/ci.yml` — restore/build/test + artifacts.
18. **Демо завершено (2 минуты)**
   - Показать, что все технологии продемонстрированы.
   - Сказать: «Это демонстрация полного цикла разработки микросервисной системы на .NET.»

## 14. FAQ (короткие ответы на типовые вопросы преподавателя)

- **Почему нет регистрации/ролей?**
  - «Это MVP. Мы сфокусировались на архитектуре, интеграции сервисов и pipeline уведомлений. Роли и JWT — следующий шаг (и его легко добавить поверх Gateway).»
- **Почему RabbitMQ, а не прямой вызов SignalR из сервиса?**
  - «Чтобы не связывать сервис с UI‑транспортом. Course Service публикует событие, а доставка в UI — отдельная ответственность.»
- **Где гарантия, что данные есть для демо?**
  - «Есть сидинг демо‑курсов: при старте сервис гарантирует несколько курсов и хотя бы по одному заданию.»


## 15. «Доказательства» 19 технологий (где лежит и как показать)

Этот раздел — самый важный: если тебя попросят “покажи доказательство технологии №N”, ты открываешь файл/адрес отсюда.

1. **ООП**
   - **Где:** `src/course-service/src/TrainingHub.CourseService.Domain/Entities/Course.cs`, `Assignment.cs`.
   - **Что сказать:** «Сущности держат инварианты: нельзя создать курс без названия, дедлайны валидируются, ValueObject `TimePeriod` следит за датами».
2. **Clean Architecture**
   - **Где:** `src/course-service/src` (проекты): `TrainingHub.CourseService.Domain.csproj`, `TrainingHub.CourseService.Application.csproj`, `TrainingHub.CourseService.Infrastructure.csproj`, `TrainingHub.CourseService.Presentation.csproj`.
   - **Что сказать:** «Зависимости направлены внутрь, инфраструктура подключается через DI.»
3. **RESTful API + Minimal API**
   - **Где:** `src/api-gateway/TrainingHub.ApiGateway/Program.cs`.
   - **Рантайм:** Swagger Gateway `http://localhost:5000/swagger/index.html`.
4. **DI / SOLID**
   - **Где:** `Program.cs` gateway + `Infrastructure/DependencyInjection.cs` course-service.
   - **Что сказать:** «Зависимости внедряем через контейнер, в коде используем интерфейсы.»
5. **CQRS + MediatR**
   - **Где:** `src/course-service/src/TrainingHub.CourseService.Application/*`.
   - **Что сказать:** «Команды и запросы отдельные, логика в handler’ах.»
6. **Git workflow**
   - **Где:** раздел 16 ниже.
7. **CI/CD (GitLab CI / GitHub Actions)**
   - **Где:** `.gitlab-ci.yml` и `.github/workflows/ci.yml`.
8. **Docker Compose**
   - **Где:** `src/ops-tooling/docker-compose.yml`.
9. **SignalR**
   - **Где:** gateway `Program.cs` (hub + endpoints), клиент `NotificationHubClient.cs`.
10. **RabbitMQ**
   - **Где:** `src/course-service/.../Infrastructure/Messaging/RabbitMqAssignmentEventPublisher.cs` и `RabbitMqAssignmentEventConsumer.cs`.
11. **Quartz**
   - **Где:** `src/course-service/src/TrainingHub.CourseService.Worker.Quartz/Jobs/DailyReminderJob.cs` + `Worker.Quartz/Program.cs`.
12. **Redis**
   - **Где:** `src/course-service/.../Infrastructure/Caching/RedisCourseCache.cs`.
13. **Blazor**
   - **Где:** `src/blazor-client/TrainingHub.BlazorClient/Pages/*.razor`.
14. **Тестирование**
   - **Где:** `src/blazor-client/TrainingHub.BlazorClient.Tests/*`, `src/api-gateway/TrainingHub.ApiGateway.Tests`.
15. **БД/JSON/Swagger**
   - **Где:** EF Core persistence в course-service + swagger в gateway.
16. **Postman/Newman**
   - **Где:** `src/ops-tooling/postman/TrainingHub.postman_collection.json`.
17. **Логирование (Serilog)**
   - **Где:** `Program.cs` + `appsettings.json` в gateway/course-service.
18. **Локализация**
   - **Где:** gateway request localization + клиентские `.resx`.
19. **ML.NET (обучение + предсказание)**
   - **Обучение:** `src/course-service/src/TrainingHub.CourseService.Worker.ML/*`.
   - **Предсказание:** `POST http://localhost:8080/analytics/assignments/predict`.

### 15.1. Как объяснять технологии «по шаблону» (чтобы не путаться)

Для любой технологии используй один и тот же шаблон (это очень помогает не теряться):

1. **Что это (1 предложение)**
2. **Зачем это в проекте (1–2 предложения, простыми словами)**
3. **Где это в коде (путь к файлу/проекту)**
4. **Как показать на демо (что открыть / что нажать / какой лог увидеть)**
5. **Типовой вопрос преподавателя и ответ (1 фраза)**

### 15.2. Подробно по всем 19 технологиям (готовые формулировки)

Ниже — разжёванное объяснение. Если тебе надо “не думать”, просто берёшь текст и говоришь.

#### 1) ООП (объектно‑ориентированное программирование)

1. **Что это:** моделирование предметной области через классы/объекты с полями и методами.
2. **Зачем:** чтобы курс и задание были не просто “таблицами”, а объектами с правилами (инвариантами).
3. **Где:**
   - `src/course-service/src/TrainingHub.CourseService.Domain/Entities/Course.cs`
   - `src/course-service/src/TrainingHub.CourseService.Domain/Entities/Assignment.cs`
4. **Как показать:** открыть `Course.cs` и показать, что курс хранит `Title/Description/Assignments`, а задания связаны с курсом.
5. **Типовой вопрос:** “Где именно ООП, а не просто DTO?” → “Сущности — это доменные объекты с логикой и инвариантами, а DTO используются только на границе API.”

#### 2) Clean Architecture

1. **Что это:** разделение кода на слои (Domain/Application/Infrastructure/Presentation) с направлением зависимостей внутрь.
2. **Зачем:** чтобы бизнес‑логика не зависела от базы данных/HTTP/очереди и её можно было тестировать и менять инфраструктуру.
3. **Где:** `src/course-service/src` (отдельные проекты `.csproj` для Domain/Application/Infrastructure/Presentation).
4. **Как показать:** в IDE открыть solution и показать, что Domain не зависит от Infrastructure, а зависимости подключаются через DI.
5. **Вопрос:** “Зачем так сложно?” → “Так проще развивать: можно заменить Redis/RabbitMQ, не переписывая домен и обработчики.”

#### 3) RESTful API (Minimal API)

1. **Что это:** стандартные HTTP‑эндпоинты (`GET/POST`) + JSON.
2. **Зачем:** Blazor и Postman должны ходить в бэкенд по понятному протоколу.
3. **Где:**
   - Gateway: `src/api-gateway/TrainingHub.ApiGateway/Program.cs`
   - Course Service: `src/course-service/src/TrainingHub.CourseService.Presentation/Program.cs`
4. **Как показать:** открыть Swagger UI `http://localhost:5000/swagger/index.html` и выполнить `GET /courses`.
5. **Вопрос:** “Почему Minimal API?” → “Он компактный и отлично подходит для учебного MVP, при этом это полноценный ASP.NET Core.”

#### 3.1) Swagger / OpenAPI (документация API)

1. **Что это:** автогенерация описания API (OpenAPI) + удобный UI (Swagger UI).
2. **Зачем:** быстро показать контракт и вызвать эндпоинты без Postman.
3. **Где:**
   - Gateway: `src/api-gateway/TrainingHub.ApiGateway/Program.cs` (`AddSwaggerGen`, `UseSwagger`, `UseSwaggerUI`)
   - Course Service: `src/course-service/src/TrainingHub.CourseService.Presentation/Program.cs` (`AddSwaggerGen`, `UseSwagger`, `UseSwaggerUI`)
4. **Как показать:**
   - `http://localhost:5000/swagger/index.html`
   - `http://localhost:8080/swagger/index.html`
   - важно: Swagger включён всегда (в коде есть комментарий `always expose Swagger...`, т.е. работает и в Docker).
5. **Вопрос:** “Почему Swagger включён в Docker?” → “Для защиты/тестирования удобнее, потому что это учебный стенд; в production обычно ограничивают доступ.”

#### 3.2) Polly (retry для устойчивости Gateway → Course Service)

1. **Что это:** библиотека resilience (повторные попытки, таймауты, circuit breaker и т.п.).
2. **Зачем:** если course-service временно недоступен (стартует медленнее/перезапуск), gateway автоматически повторит запрос.
3. **Где:**
   - `src/api-gateway/TrainingHub.ApiGateway/Program.cs`:
     - `AddHttpClient("CourseService")`
     - `.AddPolicyHandler(...WaitAndRetryAsync...)`
   - `src/api-gateway/TrainingHub.ApiGateway/appsettings.json`: `Polly:RetryCount`, `Polly:RetryDelaySeconds`
4. **Как показать:** открыть эти два файла и объяснить, что retry — конфигурируемый (параметры берутся из config).
5. **Вопрос:** “Почему это важно?” → “Это повышает устойчивость: transient ошибки не ломают пользовательский сценарий.”

#### 4) DI / SOLID

1. **Что это:** зависимости не создаются вручную, а “вкалываются” контейнером (DI). SOLID — принципы, чтобы код был расширяемым.
2. **Зачем:** чтобы легко менять реализации (например, кэш/нотификации), тестировать и не делать “спагетти”.
3. **Где:**
   - `src/course-service/src/TrainingHub.CourseService.Infrastructure/DependencyInjection.cs`
   - `src/api-gateway/TrainingHub.ApiGateway/Program.cs`
4. **Как показать:** открыть `DependencyInjection.cs` и показать регистрации `ICourseCache`, RabbitMQ publisher/consumer, HttpClient для notification gateway.
5. **Вопрос:** “А где SOLID?” → “Например, интерфейс `INotificationDispatcher` отделяет логику отправки уведомлений от места, где оно вызывается.”

#### 5) CQRS + MediatR

1. **Что это:** CQRS разделяет “чтение” и “изменение”, MediatR маршрутизирует команды/запросы к обработчикам.
2. **Зачем:** чтобы endpoint не содержал бизнес‑логику, а логика была в handler’ах и была переиспользуемой.
3. **Где:**
   - `src/course-service/src/TrainingHub.CourseService.Application/*` (папки Courses/Assignments)
   - регистрация MediatR + FluentValidation + pipeline: `src/course-service/src/TrainingHub.CourseService.Application/DependencyInjection.cs`
   - pipeline поведения валидации: `src/course-service/src/TrainingHub.CourseService.Application/Behaviors/ValidationBehavior.cs`
   - пример валидатора: `src/course-service/src/TrainingHub.CourseService.Application/Courses/CreateCourseCommand.cs` (`CreateCourseCommandValidator`)
4. **Как показать:**
   - открыть любой `*Command` и его handler, объяснить цепочку: endpoint → MediatR → handler → репозиторий/БД;
   - показать, что FluentValidation реально включён в pipeline (через `ValidationBehavior<,>`);
   - показать обработку `ValidationException` в API слое (course-service `Program.cs` возвращает `Results.ValidationProblem(...)` → это видно в Swagger как 400).
5. **Вопрос:** “Зачем MediatR, если можно просто вызвать сервис?” → “MediatR даёт единый пайплайн (валидация/логирование) и чистое разделение ответственности.”

#### 6) Git (командная работа)

1. **Что это:** система контроля версий.
2. **Зачем:** 4 человека работают параллельно и не ломают друг другу код.
3. **Где:** раздел 16 этого файла.
4. **Как показать:** показать ветки `feature/...` и PR/MR (хотя бы скрин/ссылку).
5. **Вопрос:** “Как вы делили работу?” → “По папкам/модулям, но изменения шли через PR и ревью.”

#### 7) GitLab CI/CD (пайплайн)

1. **Что это:** автоматическая сборка/тесты на сервере при каждом пуше.
2. **Зачем:** чтобы доказывать качество и автоматизировать проверку.
3. **Где:** `.gitlab-ci.yml` в корне репозитория.
4. **Как показать:** открыть `.gitlab-ci.yml` и объяснить стадии `build/test/publish`.
5. **Вопрос:** “Где деплой?” → “Для учебного проекта показываем сборку/тесты/публикацию артефактов; деплой можно добавить отдельным job.”

Дополнение про Postman в CI:

- В `.gitlab-ci.yml` есть job `postman-newman`.
- Он помечен как **manual** и `allow_failure: true`, чтобы пайплайн не падал, если в CI нет поднятого стенда.
- На защите ты говоришь: «Мы можем запускать коллекцию либо локально (smoke), либо manual job в CI, если runner развёрнут рядом со стендом.»

#### 7.1) GitHub Actions (CI на GitHub)

1. **Что это:** CI/CD workflow в GitHub.
2. **Зачем:** когда репозиторий залит на GitHub, тесты и сборка запускаются автоматически на push и PR.
3. **Где:** `.github/workflows/ci.yml`.
4. **Как показать:**
   - открыть `.github/workflows/ci.yml` и показать шаги `setup-dotnet`, `restore`, `build`, `test`;
   - сказать, что TRX результаты тестов публикуются как artifact.
5. **Вопрос:** “После заливки на GitHub надо что-то донастраивать?” → “Нет, достаточно чтобы GitHub Actions были включены в репозитории; workflow уже лежит в коде.”

#### 8) Docker / Docker Compose

1. **Что это:** контейнеры и оркестрация нескольких сервисов.
2. **Зачем:** запуск одной командой “как мини‑прод”.
3. **Где:** `src/ops-tooling/docker-compose.yml`.
4. **Как показать:** показать список сервисов (postgres/redis/rabbitmq/course-service/api-gateway/quartz/blazor) и команду `docker compose up --build`.
5. **Вопрос:** “Зачем docker, если можно dotnet run?” → “Docker даёт одинаковый запуск у всех и показывает инфраструктуру как код.”

#### 9) SignalR (real‑time)

1. **Что это:** постоянное соединение сервер↔клиент (WebSocket/Long polling под капотом).
2. **Зачем:** чтобы уведомления прилетали в UI без обновления страницы.
3. **Где:**
   - Hub/endpoint (gateway): `src/api-gateway/TrainingHub.ApiGateway/Program.cs`
     - `app.MapHub<NotificationsHub>("/hub/notifications")`
     - `POST /internal/notifications/assignments` → `hub.Clients.All.SendAsync("AssignmentScheduled", ...)`
   - Отправка уведомления в gateway (course-service):
     - `src/course-service/src/TrainingHub.CourseService.Infrastructure/Notifications/NotificationGatewayOptions.cs` (endpoint по умолчанию `/internal/notifications/assignments`)
     - `src/course-service/src/TrainingHub.CourseService.Infrastructure/Notifications/NotificationDispatcher.cs` (HTTP `PostAsJsonAsync`)
     - `src/course-service/src/TrainingHub.CourseService.Presentation/appsettings.json` → `Services:NotificationGateway:AssignmentsEndpoint`
     - `src/ops-tooling/docker-compose.yml` → `Services__NotificationGateway__BaseAddress: http://api-gateway:5000`
   - Клиент (Blazor): `src/blazor-client/TrainingHub.BlazorClient/Services/NotificationHubClient.cs` (подписка на `AssignmentScheduled`)
4. **Как показать (самый быстрый и наглядный способ):**
   - открыть UI: `http://localhost:5173/assignments`;
   - вызвать `GET http://localhost:8080/demo/rabbitmq` (через браузер/Swagger/curl);
   - вернуться в UI → появится баннер вида `HH:mm:ss SignalR: ...` (это событие из `AssignmentScheduled`).
5. **Вопрос:** “Почему не WebSocket напрямую?” → “SignalR даёт готовую модель событий, автопереподключение и удобный API для .NET.”

#### 10) RabbitMQ (очередь сообщений)

1. **Что это:** брокер сообщений: сервис публикует событие, другой сервис/компонент его потребляет.
2. **Зачем:** отделить “создание задания” от “доставки уведомления” и сделать систему устойчивее.
3. **Где:**
   - `src/course-service/src/TrainingHub.CourseService.Infrastructure/Messaging/RabbitMqAssignmentEventPublisher.cs`
   - `src/course-service/src/TrainingHub.CourseService.Infrastructure/Messaging/RabbitMqAssignmentEventConsumer.cs`
   - `src/course-service/src/TrainingHub.CourseService.Infrastructure/Messaging/RabbitMqOptions.cs`
   - consumer → HTTP уведомление в gateway:
     - `RabbitMqAssignmentEventConsumer` вызывает `_notificationDispatcher.DispatchAssignmentScheduledAsync(...)`
     - `NotificationDispatcher` делает `POST /internal/notifications/assignments`
4. **Как показать:**
   - открыть Seq: `http://localhost:8081`;
   - вызвать `GET http://localhost:8080/demo/rabbitmq` (или через Swagger);
   - в Seq по очереди показать события цепочки (удобно фильтровать по `Application`):
     - `Application = 'course-service'`:
       - `Received AssignmentScheduled event ...`
       - `Dispatched assignment notification for course ...`
     - `Application = 'api-gateway'`:
       - `Broadcasting assignment notification ...`
   - (опционально) открыть RabbitMQ Management `http://localhost:15672` и показать очередь `traininghub.notifications`.
5. **Вопрос:** “Что такое exchange/routing key простыми словами?” → “Exchange — как ‘почтовое отделение’, routing key — как ‘тема письма’, по нему письмо попадает в нужную очередь.”

#### 11) Quartz.NET (планировщик)

1. **Что это:** библиотека для задач по расписанию (cron).
2. **Зачем:** напоминания “каждый день в 18:00”, даже если пользователь ничего не нажимал.
3. **Где:**
   - `src/course-service/src/TrainingHub.CourseService.Worker.Quartz/Program.cs`
   - `src/course-service/src/TrainingHub.CourseService.Worker.Quartz/Jobs/DailyReminderJob.cs`
4. **Как показать:**
   - открыть `src/course-service/src/TrainingHub.CourseService.Worker.Quartz/Program.cs` и показать, что cron берётся из `Quartz:DailyReminderCron` (по умолчанию `0 0 18 * * ?`);
   - открыть `src/ops-tooling/docker-compose.yml` и показать, что в docker‑стенде cron переопределён:
     - `Quartz__DailyReminderCron: 0 0/1 * * * ?` (каждую минуту, чтобы не ждать 18:00).
5. **Вопрос:** “Почему отдельный воркер?” → “Чтобы фоновые задачи жили отдельно от HTTP‑сервиса и не мешали ему масштабироваться.”

Мини‑демо (на защите, 60 секунд):

1. Запусти Docker Compose.
2. Открой Seq: `http://localhost:8081`.
3. В фильтре по сообщениям найди строку:
   - `Running DailyReminderJob at ...`
4. Скажи:
   - «Quartz worker запускает job по cron. В docker‑стенде cron выставлен каждые 1 минуту, чтобы не ждать 18:00. Job берёт популярные курсы и отправляет напоминание в NotificationGateway → SignalR → UI.»

#### 12) Redis (кэш)

1. **Что это:** быстрый in-memory key-value storage.
2. **Зачем:** ускорить чтение популярных курсов (не ходить в Postgres каждый раз).
3. **Где:** `src/course-service/src/TrainingHub.CourseService.Infrastructure/Caching/RedisCourseCache.cs`.
4. **Как показать:**
   - открыть класс кэша, показать ключи/TTL;
   - вызвать `GET http://localhost:8080/demo/redis` (или через Swagger) — endpoint делает 2 запроса популярных курсов подряд;
   - в Seq (`http://localhost:8081`) показать логи:
     - `Redis cache miss: ...`
     - `Redis cache set: ...`
     - `Redis cache hit: ...`
5. **Вопрос:** “Почему Redis, а не MemoryCache?” → “Redis общий для всех экземпляров сервиса и работает в docker‑стенде как внешняя инфраструктура.”

#### 13) Blazor WebAssembly

1. **Что это:** SPA в браузере на C# (WebAssembly), без JavaScript‑фреймворков.
2. **Зачем:** показать современный .NET‑клиент и связку с SignalR.
3. **Где:** `src/blazor-client/TrainingHub.BlazorClient/Pages/*.razor`, `Program.cs`.
4. **Как показать:** открыть UI, перейти Home/Courses, создать курс/задание.
5. **Вопрос:** “Почему Blazor?” → “Можно писать фронтенд на C#, хорошо подходит под учебный .NET‑стек.”

#### 14) Тестирование (unit)

1. **Что это:** автоматические тесты кода.
2. **Зачем:** доказать, что логика работает и изменения не ломают систему.
3. **Где:**
   - `src/api-gateway/TrainingHub.ApiGateway.Tests/` (xUnit)
   - `src/blazor-client/TrainingHub.BlazorClient.Tests/` (bUnit/xUnit)
4. **Как показать:**
   - локально запустить тесты:
     - `dotnet test src/api-gateway/TrainingHub.ApiGateway.Tests/TrainingHub.ApiGateway.Tests.csproj`
     - `dotnet test src/blazor-client/TrainingHub.BlazorClient.Tests/TrainingHub.BlazorClient.Tests.csproj`
   - в CI показать job `tests` в `.gitlab-ci.yml` (он запускает эти же два проекта).
5. **Вопрос:** “А где тесты course-service?” → “Тесты UI и gateway есть. Для course-service можно расширить покрытие (следующий шаг), но архитектура CQRS/MediatR специально сделана тестопригодной.”

#### 15) PostgreSQL + EF Core (БД)

1. **Что это:** Postgres — база данных, EF Core — ORM для работы с ней.
2. **Зачем:** хранить курсы и задания как реальные данные.
3. **Где:**
   - `src/course-service/src/TrainingHub.CourseService.Infrastructure/Persistence/CourseDbContext.cs` (DbContext + `OnModelCreating`)
   - `src/course-service/src/TrainingHub.CourseService.Infrastructure/DependencyInjection.cs` (`AddDbContext` + `UseNpgsql`)
   - `src/course-service/src/TrainingHub.CourseService.Presentation/Program.cs` (создание схемы через `dbContext.Database.EnsureCreated()` при старте)
4. **Как показать:**
   - открыть `src/ops-tooling/docker-compose.yml` и показать переменную:
     - `ConnectionStrings__TrainingHub: Host=postgres;Database=traininghub;Username=traininghub;Password=traininghub`
   - открыть `CourseDbContext.cs` и показать:
     - маппинг сущностей `Course/Assignment/Notification`;
     - `OwnsOne` для value object `Period` (колонки `StartsAt/EndsAt`).
   - (опционально, самое наглядное) показать таблицы в Postgres:
     - `docker compose exec -T postgres psql -U traininghub -d traininghub -c "\\dt"`
     - `docker compose exec -T postgres psql -U traininghub -d traininghub -c "SELECT * FROM \"Courses\" LIMIT 5;"`
5. **Вопрос:** “Почему EF Core?” → “Ускоряет разработку: миграции, модели, запросы LINQ.”

#### 16) Postman (и Newman как автоматизация)

1. **Что это:** Postman — ручное тестирование API, Newman — запуск коллекции в консоли/CI.
2. **Зачем:** быстро проверять эндпоинты и показывать “автотесты API”.
3. **Где:** `src/ops-tooling/postman/TrainingHub.postman_collection.json`.
4. **Как показать:**
   - открыть коллекцию и показать запросы `GET/POST`;
   - коллекция настроена так, что `courseId` сохраняется автоматически из ответа `Create course` (то есть запускается без ручных правок);
   - (опционально) локально выполнить `newman run ...` как smoke‑тест;
   - (опционально) показать manual job `postman-newman` в `.gitlab-ci.yml`.
5. **Вопрос:** “Зачем Postman, если есть Swagger?” → “Swagger удобен для просмотра, Postman удобен как набор сохранённых сценариев и может гоняться автоматом через Newman.”

Короткий вывод “Postman сделан нормально?”

- Да: коллекция параметризована (`gateway`, `courseService`), содержит цепочку запросов и извлекает ID из ответов.
- Это важно для защиты: ты показываешь не один запрос, а сценарий “создать курс → получить курс → создать задание → проверить список”.
- Пример запуска newman локально (если нужно показать “автотест API”):
  - `newman run src/ops-tooling/postman/TrainingHub.postman_collection.json --env-var "gateway=http://localhost:5000" --env-var "courseService=http://localhost:8080"`

#### 17) Логирование (Serilog)

1. **Что это:** структурированное логирование.
2. **Зачем:** удобно видеть события (запросы, ошибки, уведомления) и доказывать работу pipeline.
3. **Где:** `Program.cs` + `appsettings.json` в gateway/course-service.
4. **Как показать:**
   - открыть Seq `http://localhost:8081`, фильтровать по `Application`.
   - показать `DemoCorrelationId` и как удобно доказывать цепочку.
   - затем создать курс/задание и показать “боевые” логи рядом с демо‑логами.
5. **Вопрос:** “Зачем Serilog?” → “Даёт структурные события, удобно фильтровать и отправлять в централизованные системы.”

#### 18) Локализация (RU/EN)

1. **Что это:** поддержка разных языков/культур.
2. **Зачем:** показать enterprise‑фичу: UI готов для разных языков.
3. **Где:**
   - Gateway: `UseRequestLocalization` в `src/api-gateway/.../Program.cs`
   - Blazor:
     - `src/blazor-client/TrainingHub.BlazorClient/Program.cs` (загрузка культуры из `localStorage` через `IJSRuntime`, установка `DefaultThreadCurrentCulture`)
     - `src/blazor-client/TrainingHub.BlazorClient/wwwroot/index.html` (JS-объект `window.blazorCulture.get/set`)
     - `src/blazor-client/TrainingHub.BlazorClient/Layout/NavMenu.razor` (кнопки `RU/EN`, сохраняют культуру и делают `forceLoad`)
     - `src/blazor-client/TrainingHub.BlazorClient/Resources/**/*.resx` (строки для страниц/меню)
4. **Как показать:**
   - в UI нажать `RU` / `EN` в верхнем меню (NavMenu) и показать, что тексты меняются без ручных правок;
   - открыть `NavMenu.razor` и показать вызов `blazorCulture.set` + `forceLoad: true`;
   - открыть любой `.resx` (например `Resources/Pages/Home.ru.resx` и `Home.en.resx`) и показать ключи `DashboardTitle`, `PopularCourses` и т.д.
5. **Вопрос:** “Почему и на сервере, и на клиенте?” → “Сервер локализует сообщения/культуру запросов, клиент локализует UI.”

#### 19) ML.NET (обучение + предсказание)

1. **Что это:** машинное обучение в .NET (ML.NET).
2. **Зачем:** показать полный цикл: обучение отдельным воркером + использование модели через API.
3. **Где:**
   - обучение: `src/course-service/src/TrainingHub.CourseService.Worker.ML/*`.
   - данные для обучения: `GET /analytics/assignments/training` (course-service).
   - предсказание: `POST /analytics/assignments/predict` (course-service).
4. **Как показать:**
   - открыть `src/course-service/src/TrainingHub.CourseService.Worker.ML/Worker.cs` и показать pipeline обучения + сохранение модели в `ModelOutputPath`;
   - открыть `src/ops-tooling/docker-compose.yml` и показать, что в docker‑стенде:
     - `Model__CourseServiceBaseAddress: http://course-service:8080`
     - `Model__ModelOutputPath: /app/models/assignment-model.zip`
     - volume `ml_models:/app/models` (модель сохраняется “на диск” в volume).
   - открыть Swagger course-service и вызвать `POST /analytics/assignments/predict`.
5. **Вопрос:** “Это реально нужно?” → “Для учебного проекта это демонстрация интеграции ML в микросервисную систему; в реальном продукте это можно развить.”

Мини‑демо ML (на защите, 90 секунд):

1. Запусти Docker Compose.
2. Открой Swagger course-service: `http://localhost:8080/swagger/index.html` и вызови:
   - `GET /analytics/assignments/training`
   Должен вернуться массив, где у части элементов `completed=true`, у части `completed=false`.
3. Открой Seq: `http://localhost:8081`.
4. Найди события ML‑воркера:
   - `ML worker fetching training data ...`
   - `ML model cross-validated accuracy: ...`
   - `Saved ML model to /app/models/assignment-model.zip`
5. В Swagger course-service вызови:
   - `POST /analytics/assignments/predict` (если модель ещё не готова — вернётся 503, это нормально).
6. Скажи:
   - «ML воркер — отдельный сервис. Он регулярно забирает тренировочные данные из course-service, обучает модель (SDCA logistic regression) и сохраняет артефакт модели. Course-service загружает эту модель из volume и отдаёт предсказание через API.»


## 16. Git workflow на 4 человека (как сделать “идеально” и понятно)

Цель раздела: чтобы на защите можно было **честно** показать командную работу в Git (ветки/коммиты/теги) и при наличии GitLab — CI и MR.

### 16.1. Структура веток

- `main` — релиз/защита.
- `develop` — *опционально*, если вы реально вели интеграцию через отдельную ветку.
- Feature‑ветки (реально удобно для 4 модулей):
  - `feature/api-gateway/<task>`
  - `feature/course-service/<task>`
  - `feature/blazor-client/<task>`
  - `feature/ops/<task>`

### 16.2. Правило «каждый отвечает за свою папку»

- Это нормально и удобно для защиты: видно вклад.
- Если у вас есть GitLab/PR (MR) — хорошо показать, что изменения проходили ревью (но **не нужно** это утверждать, если ревью не делали).

### 16.3. Как выглядит работа одного участника (шаблон)

```bash
git checkout -b feature/course-service/rabbitmq-notifications
# изменения
git add .
git commit -m "feat(course-service): publish assignment scheduled event"
git push origin feature/course-service/rabbitmq-notifications
```

Дальше:

- Если есть GitLab — создаёте MR в `main` или `develop` (в зависимости от вашей схемы).
- Если GitLab нет — показываете локально ветки и историю (см. 16.5).

### 16.4. Как сделать историю “красивой” (честно)

- Если используете PR/MR — можно мержить через **squash merge** (но только если вы реально так делали).
- Рекомендуемый стиль сообщений: Conventional Commits (`feat:`, `fix:`, `docs:`).
- Перед защитой удобно иметь **тег** (например, `v1.0-demo`), чтобы быстро показать «вот версия, которую мы защищаем».

### 16.5. Что показать на защите за 1 минуту (без GitLab тоже работает)

Открываешь терминал в корне репозитория и показываешь:

- `git branch --all` (видно feature‑ветки)
- `git log --oneline --decorate --graph -n 30` (видно историю)
- `git tag --list` (видно версию/тег для защиты)

Если есть GitLab:

- открыть `.gitlab-ci.yml` и показать job’ы `build/tests/publish/postman-newman`;
- открыть страницу Pipeline (скрин/в браузере);
- (опционально) показать MR, если он реально есть.


## 17. Траблшутинг (чтобы быстро чинить на защите)

### 17.1. `docker compose up` упал, RabbitMQ контейнер остановился

1. Сначала смотри логи:
   - `docker compose logs rabbitmq`
2. Частые причины:
   - порт `5672` или `15672` занят другим RabbitMQ;
   - недостаточно памяти Docker Desktop;
   - повреждён volume.
3. Что делать:
   - закрыть другие RabbitMQ;
   - перезапустить Docker Desktop;
   - если нужно “с нуля”: удалить volumes (делать только если не жалко данные).

### 17.2. UI не получает уведомления

Проверь:

- gateway поднят и доступен;
- в Blazor включён `NotificationHubClient` и адрес хаба правильный;
- CORS в gateway разрешает `http://localhost:5173` и `AllowCredentials`.

