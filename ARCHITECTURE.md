# Архитектура проекта

## Общая структура

Проект разделён на три основных слоя:

```
Elib2Ebook.sln
├── src/
│   ├── Elib2Ebook.Domain/            # Модели данных
│   ├── Elib2Ebook.DomainServices/     # Бизнес-логика
│   ├── Elib2EbookCli/                 # Консольное приложение
│   ├── Elib2EbookWeb/                 # Веб-приложение (Blazor)
│   └── ExternalServices/              # Интеграция с внешними сайтами
└── tests/
    └── Elib2Ebook.DomainServices.Tests/
```

---

## 1. Elib2Ebook.Domain — Модели данных

Ядро предметной области. Не имеет зависимостей от внешних сервисов.

```
Elib2Ebook.Domain/
└── Book/
    ├── Book.cs              # Книга (название, автор, обложка, главы, серия, язык)
    ├── Author.cs            # Автор (имя, ссылка)
    ├── Chapter.cs           # Глава (название, контент, изображения)
    ├── Seria.cs             # Серия книг (название, номер)
    ├── AdditionalFileCollection.cs  # Коллекция доп. файлов (аудио, книги, изображения)
    └── AdditionalTypeEnum.cs        # Типы доп. файлов: books, audio, images
└── Common/
    ├── TempFile.cs          # Временный файл (скачанное изображение и т.п.)
    ├── IdChapter.cs         # Глава, идентифицируемая по числовому ID
    └── UrlChapter.cs        # Глава, идентифицируемая по URL
```

**Ключевые особенности:**
- `Book` реализует `IDisposable` — освобождает обложку и доп. файлы
- `TempFile` — абстракция временного файла на диске с автоматическим удалением
- `AdditionalFileCollection` — коллекция доп. файлов с группировкой по типу

---

## 2. Elib2Ebook.DomainServices — Бизнес-логика

Содержит всю логику обработки книг: загрузку, сборку, работу с файлами.

```
Elib2Ebook.DomainServices/
├── Configs/
│   ├── Options.cs              # Параметры командной строки (через CommandLine)
│   └── BookGetterConfig.cs     # Конфигурация для HTTP-клиента, прокси, Flaresolverr
├── Builders/
│   ├── BuilderBase.cs          # Абстрактный базовый класс для сборщиков
│   ├── EpubBuilder.cs          # Сборка EPUB
│   ├── Fb2Builder.cs           # Сборка FB2
│   ├── CbzBuilder.cs           # Сборка CBZ (Comic Book Archive)
│   ├── JsonBuilder.cs          # Сборка JSON (полные данные)
│   ├── JsonLiteBuilder.cs      # Сборка JSON (только текст)
│   ├── TxtBuilder.cs           # Сборка TXT
│   └── AdditionaFileBuilder.cs # Сохранение дополнительных файлов
├── Getters/
│   └── GetterBase.cs           # Абстрактный базовый класс для загрузчиков
├── Misc/
│   ├── GetterFactory.cs        # Фабрика загрузчиков (reflection-based)
│   ├── BuilderProvider.cs      # Фабрика сборщиков (switch по формату)
│   └── TempFolder/             # Управление временными файлами
├── Extensions/                 # Методы расширения для различных типов
│   ├── HtmlDocumentExtensions.cs
│   ├── HttpClientExtensions.cs
│   ├── StreamExtension.cs
│   ├── StringBuilderExtensions.cs
│   ├── StringExtensions.cs
│   ├── UriExtension.cs
│   ├── FileProviderExtensions.cs
│   └── ImageFormatExtensions.cs
├── External/                   # Встроенная копия EpubSharp.dll
├── Patterns/                   # Шаблоны для EPUB (CSS, шрифты, xhtml)
├── BookNameBuilder.cs          # Построитель имени файла по шаблону
└── FileProvider.cs             # Провайдер файловой системы
```

### Архитектура сборщиков (Builders)

```
BuilderBase (abstract)
├── GetFileName(Book) → "Автор - Название.epub"
├── Build(Book)
│   ├── BuildInternal(Book, fileName) — специфичная для формата сборка
│   └── SaveCover() — если опция --cover
│
├── EpubBuilder     — создаёт EPUB через EpubSharp + CSS/шрифты
├── Fb2Builder      — создаёт FB2 через XDocument
├── CbzBuilder      — создаёт CBZ (zip-архив с изображениями)
├── JsonBuilder     — сериализует Book в JSON с полными данными
├── JsonLiteBuilder — сериализует только текст + заголовки глав
└── TxtBuilder      — создаёт простой текстовый файл
```

### Архитектура загрузчиков (Getters)

```
GetterBase (abstract)
├── SystemUrl — базовый URL сайта
├── IsSameUrl(url) — проверка поддержки URL
├── Init() — инициализация (куки, заголовки)
├── Authorize() — авторизация на сайте
├── Get(url) → Book — загрузка книги
│
└── Внешние реализации (через GetterFactory):
    Elib2Ebook.ExternalServices.*
```

**Как загрузчик определяет свою привязку к сайту:**

Каждый загрузчик переопределяет `SystemUrl`. Например:
```csharp
protected override Uri SystemUrl => new("https://author.today/");
```

`GetterFactory.Get()` через рефлексию находит все публичные неабстрактные классы, наследующие `GetterBase`, и вызывает `IsSameUrl()` для каждого. Первый подошедший возвращается.

---

## 3. ExternalServices — Внешние сервисы

Каждый внешний сайт реализован как отдельная сборка (проект) в папке `ExternalServices`. Все сборки загружаются динамически через рефлексию в `GetterFactory`.

```
ExternalServices/
├── Elib2Ebook.ExternalServices.AuthorToday/    — author.today
├── Elib2Ebook.ExternalServices.Litres/         — litres.ru
├── Elib2Ebook.ExternalServices.Litnet/         — litnet.com
├── Elib2Ebook.ExternalServices.LibSocial/      — mangalib.me, ranobelib.me, hentailib.me, v2.slashlib.me и др.
├── Elib2Ebook.ExternalServices.BooksYandex/    — books.yandex.ru (комиксы и книги)
├── Elib2Ebook.ExternalServices.Boosty/         — boosty.to
└── ...  (всего 60+ проектов)
```

**Особенности:**
- Каждый проект содержит `*Getter.cs` — класс, наследующий `GetterBase`
- Проекты могут содержать подпапки `Types/` с DTO для API
- Некоторые проекты содержат несколько геттеров (например, `LibSocial` — общая кодовая база для семейства сайтов)

---

## 4. Elib2EbookCli — Консольное приложение

Точка входа `Program.cs`:

```
Parse args (CommandLine)
  └→ Создать BookGetterConfig (HTTP-клиент, куки, прокси, Flaresolverr)
      └→ GetterFactory.Get() — получить загрузчик по URL
          ├── Init()
          ├── Authorize()
          └── Get(url) → Book
              └→ Для каждого формата:
                  BuilderProvider.Get(format) → BuilderBase
                      └── Build(Book)
```

**Поток выполнения:**
1. Парсинг аргументов командной строки
2. Создание `BookGetterConfig` — конфигурации с HTTP-клиентом
3. Получение загрузчика через `GetterFactory.Get()` (reflection-based)
4. Инициализация загрузчика (куки, заголовки)
5. Авторизация (если указаны логин/пароль)
6. Для каждого URL:
   1. Загрузка книги через `getter.Get(url)`
   2. Для каждого формата: сборка файла через `BuilderProvider.Get(format).Build(book)`
7. Сохранение дополнительных файлов (если `--additional`)

---

## 5. Elib2EbookWeb — Веб-приложение (Blazor)

Blazor Static SSR приложение.

```
Elib2EbookWeb/
├── Components/
│   ├── App.razor              # Корневой компонент
│   ├── Routes.razor           # Маршрутизация
│   ├── BookForm.razor         # Форма ввода URL и параметров
│   ├── ShortBook.razor        # Компонент для отображения краткой информации
│   └── PWA.razor              # PWA-компонент
├── Misc/
│   └── SbWriter.cs            # StringBuilder writer для веб-вывода
└── wwwroot/
    ├── app.css                # Стили
    ├── favicon.png            # Иконка
    ├── manifest.json          # PWA-манифест
    └── bootstrap/             # Bootstrap CSS
```

**Особенности:**
- Использует Blazor Static SSR (без интерактивности на клиенте)
- Страница отправляет POST-запрос с параметрами и получает готовый файл
- Поддерживает PWA для установки как приложения

---

## 6. Конфигурация сети

`BookGetterConfig` создаёт и настраивает `HttpClient`:
- **Базовый Handler**: поддержка cookies, автоматические редиректы
- **Прокси**: HTTP/HTTPS, SOCKS4, SOCKS5
- **Flaresolverr**: перенаправление запросов через Flaresolverr для обхода Cloudflare
- **Таймаут**: настраиваемый (по умолчанию 120с)
- **Заголовки**: User-Agent, Referer и др.

---

## 7. Основные паттерны

| Паттерн | Применение |
|---------|-----------|
| **Factory Method** | `GetterFactory` — создание загрузчика по URL через рефлексию |
| **Strategy** | `BuilderBase` — разные форматы сборки через единый интерфейс |
| **Template Method** | `BuilderBase.Build()` → `BuildInternal()` — общая логика сохранения + специфичная сборка |
| **Abstract Factory** | `BuilderProvider` — выбор сборщика по строке формата |

---

## 8. Сборка (Docker)

Проект публикуется в два Docker-образа:
- `onlyfart/elib2ebookcli` — CLI версия
- `onlyfart/elib2ebookweb` — Web версия (Blazor)

Оба собираются через GitHub Actions (см. `.github/workflows/dotnet.yml`). Для CLI используется **ReadyToRun + SingleFile** для portable-сборок без необходимости установки .NET Runtime.
