# 📚 Elib2Ebook

[![License](https://img.shields.io/github/license/OnlyFart/Elib2Ebook.svg?style=flat-square)](LICENSE)
[![.NET Build, Test & Publish](https://github.com/OnlyFart/Elib2Ebook/actions/workflows/dotnet.yml/badge.svg?style=flat-square)](https://github.com/OnlyFart/Elib2Ebook/actions/workflows/dotnet.yml)
[![GitHub Release](https://img.shields.io/github/v/release/OnlyFart/Elib2Ebook?style=flat-square)](https://github.com/OnlyFart/Elib2Ebook/releases/latest)
![Docker Pulls (CLI)](https://img.shields.io/docker/pulls/onlyfart/elib2ebookcli?style=flat-square&label=docker%20pulls%20(cli))
![Docker Pulls (Web)](https://img.shields.io/docker/pulls/onlyfart/elib2ebookweb?style=flat-square&label=docker%20pulls%20(web))
![GitHub Downloads (All)](https://img.shields.io/github/downloads/onlyfart/elib2ebook/total?style=flat-square)

**Elib2Ebook** — консольная утилита и веб-приложение для скачивания книг с популярных онлайн-библиотек и литературных сайтов. Поддерживает сохранение в форматах EPUB, FB2, CBZ, JSON, TXT.
---

## Оглавление

- [Возможности](#возможности)
- [Форматы](#форматы)
- [Поддерживаемые сайты](#поддерживаемые-сайты)
- [Установка](#установка)
  - [Консольная утилита](#консольная-утилита)
  - [Docker (CLI)](#docker-cli)
  - [Docker (Web)](#docker-web)
  - [Сборка из исходников](#сборка-из-исходников)
- [Использование](#использование)
  - [Обязательные параметры](#обязательные-параметры)
  - [Авторизация](#авторизация)
  - [Сеть и прокси](#сеть-и-прокси)
  - [Сохранение файлов](#сохранение-файлов)
  - [Главы](#главы)
  - [Изображения](#изображения)
  - [Дополнительные файлы](#дополнительные-файлы)
  - [Временные файлы](#временные-файлы)
  - [Шаблон имени файла](#шаблон-имени-файла)
- [Архитектура проекта](#архитектура-проекта)
- [Полный список параметров](#полный-список-параметров)
- [Скриншоты](#скриншоты)
- [Лицензия](#лицензия)

---

## Возможности

- **100+ сайтов** — ранобэ, фанфики, комиксы, манга, классическая литература
- **Несколько форматов одновременно** — сохраняйте книгу сразу в EPUB, FB2 и TXT
- **Гибкая загрузка глав** — по номеру (в том числе с конца) или по названию
- **Авторизация** — скачивание платных и закрытых книг
- **Обложка** — сохранение отдельным файлом
- **Режим «только текст»** — без загрузки изображений
- **Дополнительные файлы** — аудиоверсии, оригиналы книг
- **Прокси** — HTTP/HTTPS, SOCKS4, SOCKS5
- **Flaresolverr** — обход Cloudflare
- **Шаблон имени** — настраиваемый формат выходного файла
- **Docker** — готовые образы для CLI и Web-версии (Blazor)

---

## Форматы

| Формат       | Описание                            |
|:-------------|:------------------------------------|
| `epub`       | EPUB (электронные книги)            |
| `fb2`        | FictionBook 2.0                     |
| `cbz`        | Comic Book Archive                  |
| `json`       | JSON (полные данные)                |
| `json_lite`  | JSON (только текст и заголовки глав)|
| `txt`        | Обычный текст                       |

---

## Поддерживаемые сайты

<details>
<summary><b>Нажмите, чтобы развернуть список</b> (100+ сайтов)</summary>

```
https://acomics.ru/
https://author.today/
https://bigliba.com/
https://bllate.org/
https://bookinbook.ru/
https://bookhamster.ru/
https://bookinist.pw/
https://bookmate.ru/
https://booknet.com/
https://booknet.ua/
https://bookriver.ru/
https://books.yandex.ru/
https://bookstab.ru/
https://bookstime.ru/
https://bookuruk.com/
https://boovell.ru/
https://boosty.to/
https://dark-novels.ru/
https://desu.me/
https://dreame.com/
https://erolate.com/
https://eznovels.com/
https://fanficus.com/
https://fb2.top/
https://ficbook.net/
https://fictionbook.ru/
https://freedlit.space/
https://hentailib.me/
https://hogwartsnet.ru/
https://hotnovelpub.com/
https://hub-book.com/
https://i-gram.ru/
https://ifreedom.su/
https://jaomix.ru/
https://ladylib.top/
https://lanovels.com/
https://libbox.ru/
https://libking.ru/
https://librebook.me/
https://libst.ru/
https://lightnoveldaily.com/
https://litgorod.ru/
https://litlife.club/
https://litmarket.ru/
https://litmir.me/
https://litnet.com/
https://litres.ru/
https://litsovet.ru/
https://manga.ovh/
https://mangalib.me/
https://mangalib.org/
https://mangamammy.ru/
https://mir-knig.com/
https://mlate.ru/
https://mybook.ru/
https://neobook.org/
https://novelhall.com/
https://noveltranslate.com/
https://novelxo.com/
https://online-knigi.com.ua/
https://prodaman.ru/
https://ranobe-novels.ru/
https://ranobe.ovh/
https://ranobehub.org/
https://ranobelib.me/
https://ranobes.com/
https://readli.net/
https://readmanga.live/
https://remanga.org/
https://renovels.org/
https://romfant.ru/
https://royalroad.com/
https://ru.novelxo.com/
https://samlib.ru/
https://stroki.mts.ru/
https://tl.rulate.ru/
https://topliba.com/
https://twilightrussia.ru/
https://v2.slashlib.me/
https://wattpad.com/
https://wuxiaworld.ru/
https://younettranslate.com/
https://ранобэ.рф/
```
</details>

---

## Установка

### Консольная утилита

**Вариант 1 — Portable (рекомендуется)**

Скачайте последнюю portable-версию со [страницы релизов](https://github.com/OnlyFart/Elib2Ebook/releases/latest).  
Portable-сборки **не требуют установки .NET Runtime** (спасибо [@alfeg](https://github.com/alfeg) за настройку сборки).

**Вариант 2 — Non-portable**

Необходим [.NET Runtime 10.0+](https://dotnet.microsoft.com/en-us/download/dotnet/10.0).

### Docker (CLI)

```bash
docker run --rm \
  -v /путь/к/папке:/Save \
  onlyfart/elib2ebookcli \
  -u https://author.today/work/212721 \
  -f epub,fb2 \
  --save /Save
```

### Docker (Web)

```bash
docker run --rm \
  -p 8080:8080 \
  onlyfart/elib2ebookweb
```

После запуска откройте в браузере [http://localhost:8080](http://localhost:8080).

### Сборка из исходников

```bash
git clone https://github.com/OnlyFart/Elib2Ebook.git
cd Elib2Ebook

dotnet publish Elib2EbookCli/Elib2EbookCli.csproj \
  -c Release \
  -o ./publish/cli

dotnet publish Elib2EbookWeb/Elib2EbookWeb.csproj \
  -c Release \
  -o ./publish/web
```

---

## Использование

### Обязательные параметры

```bash
# Одна книга, один формат
Elib2EbookCli --url https://author.today/work/212721 --format epub

# Одна книга, несколько форматов сразу
Elib2EbookCli --url https://author.today/work/212721 --format epub,fb2,txt

# Несколько книг одной командой (через запятую)
Elib2EbookCli --url https://author.today/work/212721,https://litnet.com/book/12345 --format epub
```

### Авторизация

```bash
# Базовая авторизация (логин + пароль)
Elib2EbookCli -u https://author.today/work/212721 -f epub --login vasya --password pupkin
```

### Сеть и прокси

```bash
# HTTP-прокси
Elib2EbookCli -u https://author.today/work/212721 -f epub --proxy http://proxy.example.com:8080

# SOCKS5-прокси
Elib2EbookCli -u https://author.today/work/212721 -f epub --proxy socks5://proxy.example.com:1080

# Flaresolverr (обход Cloudflare)
Elib2EbookCli -u https://author.today/work/212721 -f epub --flare http://localhost:8191

# Увеличенный таймаут (300 секунд)
Elib2EbookCli -u https://author.today/work/212721 -f epub --timeout 300

# Задержка между запросами (2 секунды)
Elib2EbookCli -u https://author.today/work/212721 -f epub --delay 2
```

### Сохранение файлов

```bash
# Сохранить в указанную папку
Elib2EbookCli -u https://author.today/work/212721 -f epub --save "C:\Books"

# Сохранить обложку отдельным файлом
Elib2EbookCli -u https://author.today/work/212721 -f epub --cover
```

### Главы

```bash
# Диапазон глав (с 3-й по 10-ю)
Elib2EbookCli -u https://author.today/work/212721 -f epub --start 3 --end 10

# Только первые 5 глав (без указания end)
Elib2EbookCli -u https://author.today/work/212721 -f epub --end 5

# Только указанная глава
Elib2EbookCli -u https://author.today/work/212721 -f epub --start 7 --end 7

# Начиная с 5-й главы и до конца
Elib2EbookCli -u https://author.today/work/212721 -f epub --start 5

# Последние 3 главы (отрицательный индекс)
Elib2EbookCli -u https://author.today/work/212721 -f epub --start -3

# Только предпоследняя глава
Elib2EbookCli -u https://author.today/work/212721 -f epub --start -2 --end -1

# По названиям глав
Elib2EbookCli -u https://author.today/work/212721 -f epub --start-name "Глава 3" --end-name "Глава 10"

# Без загрузки глав (только обложка и метаданные)
Elib2EbookCli -u https://author.today/work/212721 -f epub --no-chapters
```

### Изображения

```bash
# Не загружать изображения (только текст)
Elib2EbookCli -u https://author.today/work/212721 -f epub --no-image
```

### Дополнительные файлы

```bash
# Сохранить все дополнительные файлы (аудио, оригиналы книг, изображения)
Elib2EbookCli -u https://author.today/work/212721 -f epub --additional

# Сохранить только аудиоверсии
Elib2EbookCli -u https://author.today/work/212721 -f epub --additional --additional-types audio

# Сохранить аудио и книги (без изображений)
Elib2EbookCli -u https://author.today/work/212721 -f epub --additional --additional-types audio,books
```

### Временные файлы

```bash
# Указать папку для временных файлов
Elib2EbookCli -u https://author.today/work/212721 -f epub --temp "C:\Temp\Elib2Ebook"

# Не удалять временные файлы после завершения (для отладки)
Elib2EbookCli -u https://author.today/work/212721 -f epub --save-temp
```

### Шаблон имени файла

```bash
# Использовать шаблон по умолчанию: "Автор - Название.epub"
Elib2EbookCli -u https://author.today/work/212721 -f epub

# Только название книги
Elib2EbookCli -u https://author.today/work/212721 -f epub --book-name-pattern "{Book.Title}"

# Название + серия
Elib2EbookCli -u https://author.today/work/212721 -f epub --book-name-pattern "{Book.Title} [{Seria.Name}]"

# Полный шаблон
Elib2EbookCli -u https://author.today/work/212721 -f epub --book-name-pattern "{Author.Name} - {Book.Title} [{Seria.Name}]"
```

---

## Архитектура проекта

Подробное описание архитектуры проекта, структуры слоёв и используемых паттернов см. в [ARCHITECTURE.md](ARCHITECTURE.md).

---

## Полный список параметров

| Параметр                | Алиас | Обязательный | По умолчанию                         | Описание                                                      |
|-------------------------|:-----:|:------------:|:------------------------------------:|---------------------------------------------------------------|
| `--url`                 | `-u`  | ✅           | —                                    | Ссылка на книгу (несколько — через запятую)                   |
| `--format`              | `-f`  | ✅           | —                                    | Формат(ы): `epub`, `fb2`, `cbz`, `json`, `json_lite`, `txt`  |
| `--login`               | `-l`  | ❌           | —                                    | Логин для авторизации                                         |
| `--password`            | `-p`  | ❌           | —                                    | Пароль для авторизации                                        |
| `--proxy`               | —     | ❌           | —                                    | Прокси: `(http\|socks4\|socks5)://host:port`                 |
| `--flare`               | —     | ❌           | —                                    | Адрес Flaresolverr (обход Cloudflare)                         |
| `--save`                | `-s`  | ❌           | Текущая директория                   | Путь для сохранения книги                                     |
| `--cover`               | `-c`  | ❌           | `false`                              | Сохранить обложку отдельным файлом                            |
| `--timeout`             | `-t`  | ❌           | `120`                                | Таймаут HTTP-запросов в секундах                              |
| `--delay`               | `-d`  | ❌           | `0`                                  | Задержка между запросами в секундах                           |
| `--no-image`            | —     | ❌           | `false`                              | Не загружать изображения                                      |
| `--start`               | —     | ❌           | —                                    | Номер первой главы (отрицательное — с конца)                  |
| `--end`                 | —     | ❌           | —                                    | Номер последней главы (отрицательное — с конца)               |
| `--start-name`          | —     | ❌           | —                                    | Название первой главы                                         |
| `--end-name`            | —     | ❌           | —                                    | Название последней главы                                      |
| `--no-chapters`         | —     | ❌           | `false`                              | Не загружать главы                                            |
| `--temp`                | —     | ❌           | Системная временная папка            | Директория для временных файлов                               |
| `--save-temp`           | —     | ❌           | `false`                              | Не удалять временные файлы после завершения                   |
| `--additional`          | —     | ❌           | `false`                              | Сохранить дополнительные файлы (аудио, оригиналы)             |
| `--additional-types`    | —     | ❌           | Все типы                             | Типы доп. файлов: `books`, `audio`, `images`                  |
| `--book-name-pattern`   | —     | ❌           | `{Author.Name} - {Book.Title}`       | Шаблон имени выходного файла                                  |

---

## Скриншоты

<img width="821" height="691" alt="image" src="https://github.com/user-attachments/assets/b124ef56-59a5-47db-9e3a-94e5e3451ca2" />


---

## Лицензия

Распространяется под лицензией [MIT](LICENSE).
