# Структура кода проекта

## Обзор архитектуры

Проект Bimload построен по принципам **Clean Architecture** и **Test-Driven Development (TDD)**. Код разделен на три основных проекта:

1. **Bimload.Core** — бизнес-логика и модели данных
2. **Bimload.Gui** — пользовательский интерфейс (Windows Forms)
3. **Bimload.Tests** — модульные и интеграционные тесты

## Структура решения

```
bimload/
├── Bimload.sln                    # Solution файл
├── Bimload.Core/                  # Основная бизнес-логика
│   ├── Models/                    # Модели данных
│   ├── Parsers/                   # Парсеры конфигурации
│   ├── Services/                  # Бизнес-сервисы
│   │   └── Http/                  # HTTP операции
│   └── Logging/                   # Интерфейсы логирования
├── Bimload.Gui/                   # Windows Forms приложение
│   ├── Forms/                     # Формы
│   ├── Services/                  # GUI-специфичные сервисы
│   └── Logging/                   # Реализация логирования для UI
├── Bimload.Tests/                 # Тесты
│   └── Core/                      # Тесты для Core проекта
├── creds/                         # Папка с конфигурационными файлами
│   └── *.ini                      # Конфигурационные файлы
└── update_info.json               # Файл состояния приложения
```

## Проект Bimload.Core

### Назначение

Содержит всю бизнес-логику приложения, независимую от UI. Может быть использован в других типах приложений (консольных, веб-приложениях и т.д.).

### Структура папок

#### `Models/` — Модели данных

**Credentials.cs**
- Модель конфигурации программы
- Свойства: `LocalPath`, `ProductName`, `FileVersionPattern`, `ProductVersionPattern`, `HttpUrl`, `HttpPattern`

**InstalledProgram.cs**
- Информация об установленной программе
- Свойства: `Name`, `Version`, `InstallLocation`

**UpdateResult.cs**
- Результат операции обновления
- Свойства: `Status`, `OldVersion`, `NewVersion`

#### `Parsers/` — Парсеры

**ICredentialsParser.cs** (интерфейс)
- Определяет метод `Parse(string iniContent)` для парсинга `.ini` файлов

**CredentialsParser.cs** (реализация)
- Парсит содержимое `.ini` файла
- Игнорирует комментарии (строки с `#`)
- Поддерживает только HTTP-поля (FTP поля игнорируются)
- Обрабатывает экранирование путей Windows

#### `Services/` — Бизнес-сервисы

**IVersionService.cs** / **VersionService.cs**
- Извлечение версии из имени файла (`ExtractVersionFromFileName`)
- Извлечение версии из версии продукта (`ExtractVersionFromProductVersion`)
- Сравнение версий (`CompareVersions`)

**IWmiQueryWrapper.cs** / **WmiQueryWrapper.cs**
- Абстракция для WMI-запросов
- Позволяет мокировать WMI в тестах
- Метод: `GetManagementObjects(string query)`

**IWmiService.cs** / **WmiService.cs**
- Работа с установленными программами через WMI
- Методы:
  - `GetLatestInstalledProgram(string productName)` — поиск последней установленной версии
  - `GetManagementObjectForProgram(InstalledProgram program)` — получение WMI объекта для программы

**IHttpClient.cs** / **HttpClientWrapper.cs** (в `Http/`)
- HTTP операции
- Методы:
  - `GetLatestFileAsync(string httpUrl, string httpPattern)` — получение последнего файла с сервера
  - `DownloadFileAsync(string url, string localFilePath)` — скачивание файла

**IProgramInstaller.cs** / **ProgramInstaller.cs**
- Установка и удаление программ
- Методы:
  - `InstallProgramAsync(string filePath)` — установка программы с параметром `/quiet`
  - `UninstallProgramAsync(InstalledProgram program)` — удаление через WMI

**IUpdateService.cs** / **UpdateService.cs**
- Основной сервис обновления
- Координирует работу всех других сервисов
- Метод: `UpdateAsync(Credentials credentials)` — полный цикл обновления

#### `Logging/` — Логирование

**ILogger.cs**
- Интерфейс для логирования
- Уровни: `Info`, `Error`, `Warning`, `Success`
- Метод: `Log(string message, LogLevel level)`

## Проект Bimload.Gui

### Назначение

Windows Forms приложение, предоставляющее графический интерфейс для работы с приложением.

### Структура папок

#### `Forms/` — Формы

**MainForm.cs**
- Главная форма приложения
- Содержит:
  - `DataGridView` — таблица программ
  - `RichTextBox` — панель логов
  - Кнопки управления
  - `SplitContainer` — разделение таблицы и логов

**Основные методы:**
- `InitializeComponent()` — инициализация UI
- `LoadConfigurations()` — загрузка конфигураций из `.ini` файлов
- `ToggleAllButton_Click()` — переключение всех чекбоксов
- `UpdateButton_Click()` — запуск обновления выбранных программ
- `FindProjectRoot()` — поиск корня проекта для определения пути к `creds/` (ищет в папке с exe и в корне проекта)

#### `Services/` — GUI-сервисы

**ConfigurationLoader.cs**
- Загрузка конфигураций из папки `creds/`
- Метод: `LoadConfigurations(string credsFolderPath)`
- Возвращает список `ConfigurationItem`

**StateManager.cs**
- Сохранение и загрузка состояния выбранных программ
- Использует JSON для сериализации
- Методы:
  - `SaveState(List<ProgramState> states)`
  - `LoadState()`

#### `Logging/` — Логирование для UI

**RichTextBoxLogger.cs**
- Реализация `ILogger` для вывода в `RichTextBox`
- Цветной вывод с временными метками
- Потокобезопасный вывод через `Invoke()`

#### `Program.cs`
- Точка входа приложения
- Инициализация зависимостей
- Запуск главной формы

## Проект Bimload.Tests

### Назначение

Модульные и интеграционные тесты для всех компонентов проекта.

### Структура папок

```
Bimload.Tests/
└── Core/
    ├── Models/                    # Тесты моделей
    ├── Parsers/                   # Тесты парсеров
    ├── Services/                  # Тесты сервисов
    │   └── Http/                  # Тесты HTTP операций
    └── Logging/                   # Тесты логирования
```

### Используемые библиотеки

- **xUnit** — фреймворк для тестирования
- **Moq** — мокирование зависимостей
- **FluentAssertions** — утверждения в стиле fluent API

### Примеры тестов

**CredentialsParserTests.cs**
- Парсинг валидного `.ini` файла
- Обработка комментариев
- Обработка отсутствующих полей
- Игнорирование FTP полей

**VersionServiceTests.cs**
- Извлечение версии из имени файла
- Извлечение версии из версии продукта
- Сравнение версий

**WmiServiceTests.cs**
- Поиск установленных программ (с моками)
- Сортировка по версии
- Обработка отсутствия программ

**UpdateServiceTests.cs**
- Полный цикл обновления (интеграционные тесты)
- Пропуск обновления при актуальной версии
- Обработка ошибок

## Принципы проектирования

### Dependency Injection (DI)

Все зависимости передаются через конструкторы:

```csharp
public UpdateService(
    IWmiService wmiService,
    IVersionService versionService,
    IHttpClient httpClient,
    IProgramInstaller programInstaller,
    ILogger logger)
```

### Interface Segregation

Каждый сервис имеет свой интерфейс:
- `IWmiService`, `IVersionService`, `IHttpClient`, `IProgramInstaller`, `ILogger`

### Single Responsibility

Каждый класс отвечает за одну задачу:
- `WmiService` — только работа с WMI
- `VersionService` — только работа с версиями
- `HttpClientWrapper` — только HTTP операции

### Testability

Все зависимости абстрагированы через интерфейсы, что позволяет легко мокировать их в тестах.

## Потоки данных

### Процесс обновления программы:

```
MainForm.UpdateButton_Click()
    ↓
UpdateService.UpdateAsync()
    ↓
WmiService.GetLatestInstalledProgram()
    ↓ (WMI запрос)
WmiQueryWrapper.GetManagementObjects()
    ↓
VersionService.ExtractVersionFromProductVersion()
    ↓
HttpClientWrapper.GetLatestFileAsync()
    ↓ (HTTP запрос)
HttpClient.GetAsync()
    ↓
VersionService.ExtractVersionFromFileName()
    ↓
VersionService.CompareVersions()
    ↓ (если нужно обновление)
HttpClientWrapper.DownloadFileAsync()
    ↓
ProgramInstaller.UninstallProgramAsync()
    ↓ (WMI Uninstall)
ProgramInstaller.InstallProgramAsync()
    ↓ (Process.Start)
UpdateResult
```

## Зависимости между проектами

```
Bimload.Gui
    ├──→ Bimload.Core (ссылка)
    └──→ System.Windows.Forms (NuGet)

Bimload.Core
    ├──→ System.Management (NuGet) - для WMI
    └──→ System.Net.Http (встроено) - для HTTP

Bimload.Tests
    ├──→ Bimload.Core (ссылка)
    ├──→ Bimload.Gui (ссылка)
    ├──→ xunit (NuGet)
    ├──→ Moq (NuGet)
    └──→ FluentAssertions (NuGet)
```

## Обработка ошибок

Все сервисы используют исключения для обработки ошибок:

- `ArgumentNullException` — для null параметров
- `ArgumentException` — для невалидных параметров
- `FileNotFoundException` — для отсутствующих файлов
- `HttpRequestException` — для ошибок HTTP
- `InvalidOperationException` — для ошибок операций (установка, удаление)

## Асинхронность

Все сетевые операции и операции с процессами выполняются асинхронно:

- `async Task` методы
- `await` для асинхронных операций
- `Task.CompletedTask` для синхронных операций, обернутых в async

## Расширяемость

Архитектура позволяет легко:

1. **Добавить новые источники дистрибутивов**
   - Создать новый интерфейс (например, `IFtpClient`)
   - Реализовать его
   - Интегрировать в `UpdateService`

2. **Изменить UI**
   - Заменить Windows Forms на WPF или другой UI фреймворк
   - Бизнес-логика в `Bimload.Core` остается неизменной

3. **Добавить новые форматы конфигурации**
   - Создать новый парсер (например, `IJsonCredentialsParser`)
   - Реализовать его
   - Использовать в `ConfigurationLoader`

## Тестирование

Проект полностью покрыт тестами (36 тестов):

- **Модели**: проверка создания и свойств
- **Парсеры**: проверка парсинга различных форматов
- **Сервисы**: проверка бизнес-логики с моками
- **HTTP**: проверка сетевых операций с моками HttpClient
- **WMI**: проверка работы с WMI через моки

Все тесты проходят успешно и обеспечивают уверенность в корректности работы кода.


