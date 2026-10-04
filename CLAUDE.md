# CLAUDE.md

Монорепозиторий мессенджера. Бэкенд (в корне): .NET 10, ASP.NET Core, MongoDB, Mediator (source generator, не MediatR). Фронтенд — `frontend/`: React + TypeScript + Vite.
Модульный монолит, внутри модулей — DDD. Как и почему всё устроено — в [README.md](README.md); здесь — правила, которым новый код **обязан** следовать.

Не отступай от этой архитектуры молча. Если задача не укладывается в правила — остановись и предложи вариант пользователю.

## Сборка

```bash
dotnet build Messenger.sln
```

В песочнице Claude обычная сборка зависает (MSBuild не может связаться со своими рабочими процессами):

```bash
dotnet build Messenger.sln -m:1 -nodeReuse:false -p:UseSharedCompilation=false -p:NuGetAudit=false
```

`dotnet run` в песочнице тоже зависает на restore — сначала собери с флагами выше, потом `dotnet run --no-build`.

Ожидаемое предупреждение: `MSG0005` для `AccountRegistered` (у события пока нет подписчиков). Других быть не должно.

Тесты:

```bash
dotnet test Messenger.Users.Tests --no-build
dotnet test Messenger.Chats.Tests --no-build
dotnet test Messenger.Files.Tests --no-build
```

Фронтенд:

```bash
cd frontend
npm ci
npm run lint
npm run build
npm run build:host   # сборка в MessengerWeb/wwwroot: фронт раздаёт бэкенд
```

## Структура

- `frontend` — фронтенд, отдельный npm-проект; в `.sln` не входит.
- `MessengerWeb` — хост: `Program.cs`, контроллеры, MCP-инструменты (`Mcp/`), `DomainExceptionHandler`. Бизнес-логики здесь нет: и контроллеры, и MCP-инструменты только отправляют команды в Mediator.
- `Messenger.<Module>.Tests` — unit-тесты модуля (xUnit); новые правила домена покрывай тестами там.
- `Messenger.Users.Contracts` — `IUsersApi` для других модулей; реализация в `Messenger.Users`. Так же `Messenger.Files.Contracts` — `IFilesApi`, реализация в `Messenger.Files`.
- `Messenger.Infrastructure.Mongo` — общая техника (`AddMongo()`): клиент, база, сериализатор `Guid`. Ничего не знает о предметной области.
- `Messenger.<Module>` — модуль = bounded context, один проект с папками `Domain` / `Application` / `Infrastructure` и `<Module>Module.cs`. Делить модуль на несколько проектов **не нужно** — решение принято.
- `Dockerfile` — образ с бэкендом и собранным фронтом, без MongoDB (база внешняя). Новый проект, на который ссылается `MessengerWeb`, впиши в слой restore (`COPY …csproj`), иначе образ не соберётся.

## Правила архитектуры

### Модули
- Новая предметная область — **новый модуль**, а не папка в существующем.
- Модуль владеет своими данными: читает и пишет **только свои** коллекции. Имена коллекций — константы в репозиториях модуля.
- Модули общаются только через `Messenger.<Module>.Contracts`: публичные запросы (интерфейс вроде `IUsersApi`) и интеграционные события. В контрактах — только примитивы (`Guid`, `string`, …), никаких доменных типов.
- **Нельзя** ссылаться на `Domain`, `Application` или `Infrastructure` чужого модуля, использовать его репозитории или подписываться на его доменные события.

### Пользователи в других модулях
- Пользователь — это `UserId` (тот же `Guid`, что `Account.Id`). Общего класса `User` нет и не будет.
- Другие модули не используют `Account` и `UserProfile`. Им нужна своя модель с тем, что важно в их контексте (например, `ChatMember { UserId, Role, IsMuted, JoinedAt }` в Chats).
- Не копируй имя/аватар пользователя в документы других модулей (например, в сообщения). Храни `UserId`; данные профиля подставляются при чтении (пакетный запрос профилей).

### Domain
- Никаких зависимостей от Mongo, ASP.NET и прочей инфраструктуры. Единственное допустимое исключение — `IDomainEvent : INotification` из `Mediator.Abstractions`.
- Агрегаты наследуют `AggregateRoot`, создаются только фабричными методами (`Account.Register`, `UserProfile.Create`), конструктор приватный, сеттеры `private set`.
- Бизнес-правила и инварианты — в методах агрегатов и в value objects. Не в handler-ах, не в контроллерах.
- Значения с правилами (email, имена, …) — value objects: `sealed record`, валидация и нормализация в конструкторе, `ToString()` возвращает значение.
- Ошибки домена — наследники `DomainException`. Новый тип, которому нужен код ответа кроме 400, добавь в маппинг `MessengerWeb/DomainExceptionHandler.cs`.
- Доменные события — `sealed record`, реализуют `IDomainEvent`, поднимаются агрегатом через `Raise(...)`.

### Application
- Handler только координирует: загрузить → вызвать метод модели → сохранить → `PublishDomainEvents` (строго **после** сохранения).
- Команды и запросы — `sealed record`, реализуют `IRequest<T>` / `IRequest`. Handler-ы — `sealed`.
- Валидация входа — DataAnnotations на команде или request-DTO; лимиты бери из констант домена (`Email.MaxLength`, `DisplayName.MaxLength`), не дублируй числа.
- `CancellationToken` пробрасывай до репозиториев.

### Infrastructure и Mongo
- Весь BSON-маппинг — в `Infrastructure` модуля (`<Module>BsonMappings`): `BsonClassMap`, регистрация сериализаторов. Атрибуты `[Bson*]` в доменных классах запрещены.
- Value objects хранятся **плоскими строками** через свой `SerializerBase<T>`, а не вложенными документами. Фильтры по ним строй через `Builders<T>.Filter.Eq(...)`.
- Регистрация маппинга должна быть идемпотентной (`TryRegisterSerializer`, проверка `IsClassMapRegistered`).
- Не переименовывай коллекции, поля и индексы без миграции существующих данных (коллекция аккаунтов исторически называется `users` — так и оставь).
- Уникальность обеспечивает индекс в базе; нарушение (`DuplicateKey`) репозиторий переводит в доменное исключение.
- Меняешь то, как тип ложится в Mongo, — проверь форму документа без базы: `entity.ToBsonDocument()`, `BsonSerializer.Deserialize<T>(doc)` и `filter.Render(...)` во временном консольном проекте вне репозитория.

### Хост
- Модуль подключается в `Program.cs`: сначала `AddMongo(configuration)` и `AddMediator(...)`, затем `Add<Module>()`, после `Build()` — `Initialize<Module>()`.
- `Mediator.SourceGenerator` подключён **только** в `MessengerWeb`: он находит handler-ы во всех модулях. В модулях — только `Mediator.Abstractions`; `AddMediator` в модуле не вызывай.
- Контроллеры тонкие: собрать команду → `_mediator.Send` → вернуть результат.
- Все эндпоинты требуют вход (fallback policy). Анонимные помечай `[AllowAnonymous]` явно.
- Текущий аккаунт — только `User.GetAccountId()`. Не принимай id вызывающего из тела запроса или маршрута; проверки «можно ли ему» — в методах агрегата (`EnsureProfileEditableBy` и т. п.).

### Фронтенд
- Типы API — только из сгенерированной `frontend/src/api/schema.d.ts`, запросы — через `api` из `src/api/client.ts`. Руками типы ответов не описывай, схему не правь.
- Изменил API — запусти бэкенд, `npm run api:generate` в `frontend/` и закоммить схему в той же ветке.
- Действия контроллеров возвращают `ActionResult<T>` (или `IActionResult` для 204), иначе в Swagger не будет схемы ответа.
- Лимиты полей — в `frontend/src/limits.ts`, синхронно с константами домена.

## Стиль кода

Пиши как окружающий код:
- file-scoped namespace, совпадающий с папкой (`Messenger.Users.Application`);
- зависимости через конструктор в `private readonly` поля (без primary constructors);
- длинные сигнатуры и вызовы — по одному параметру на строку;
- проверки — `== null` / `!= null`;
- комментарии редкие, только про неочевидное «почему».

## Git

- Коммить только по просьбе пользователя. Работай в ветке (`fix/...`, `refactor/...`), в `main` — только через PR на GitHub: аппрув и merge (merge commit), когда пользователь попросит. Напрямую в `main` не пушь.
- Сообщения коммитов — на английском: заголовок + список изменений.
- `obj/`, `bin/`, `.idea/`, `node_modules/`, `dist/` не коммитятся.
- Изменение API и фронта под него — в одной ветке и одном PR.
