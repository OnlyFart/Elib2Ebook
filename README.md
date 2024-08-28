# Elib2Ebook
[![GitHub License](https://img.shields.io/github/license/OnlyFart/Elib2Ebook.svg?style=flat-square)](https://github.com/OnlyFart/Elib2Ebook/blob/master/LICENSE)
[![GitHub Stars](https://img.shields.io/github/stars/OnlyFart/Elib2Ebook.svg?style=flat-square)](https://github.com/OnlyFart/Elib2Ebook/stargazers)
[![GitHub forks](https://img.shields.io/github/forks/OnlyFart/Elib2Ebook.svg?style=flat-square)](https://github.com/OnlyFart/Elib2Ebook/network)
[![GitHub tag](https://img.shields.io/github/v/tag/OnlyFart/Elib2Ebook.svg?style=flat-square)](https://github.com/OnlyFart/Elib2Ebook/releases/latest)
![GitHub downloads](https://img.shields.io/github/downloads/onlyfart/elib2ebook/total?style=flat-square)



Инструмент для сохранения любой доступной книги со следующих сайтов в форматах epub, fb2, cbz:
<details>
<pre>
* http://samlib.ru/
* https://acomics.ru/
* https://author.today/
* https://bigliba.com/
* https://bookinbook.ru/
* https://bookhamster.ru/
* https://bookinist.pw/
* https://booknet.com/
* https://booknet.ua/
* https://bookriver.ru/
* https://bookstab.ru/
* https://bookstime.ru/
* https://bookuruk.com/
* https://dark-novels.ru/
* https://desu.me/
* https://dreame.com/
* https://erolate.com/
* https://eznovels.com/
* https://fb2.top/
* https://ficbook.net/
* https://fictionbook.ru/
* https://hentailib.me/
* https://hogwartsnet.ru/
* https://hotnovelpub.com/
* https://hub-book.com/
* https://i-gram.ru/
* https://ifreedom.su/
* https://jaomix.ru/
* https://ladylib.top/
* https://lanovels.com/
* https://libbox.ru/
* https://libst.ru/
* https://lightnoveldaily.com/
* https://litgorod.ru/
* https://litmarket.ru/
* https://litmir.me/
* https://litnet.com/
* https://litres.ru/
* https://litsovet.ru/
* https://manga.ovh/
* https://mangalib.me/
* https://mangamammy.ru/
* https://mir-knig.com/
* https://mlate.ru/
* https://mybook.ru/
* https://neobook.org/
* https://noveltranslate.com/
* https://novelxo.com/
* https://online-knigi.com.ua/
* https://prodaman.ru/
* https://ranobe-novels.ru/
* https://ranobe.ovh/
* https://ranobehub.org/
* https://ranobelib.me/
* https://ranobes.com/
* https://readli.net/
* https://readmanga.live/
* https://remanga.org/
* https://renovels.org/
* https://romfant.ru/
* https://royalroad.com/
* https://ru.novelxo.com/
* https://tl.rulate.ru/
* https://topliba.com/
* https://twilightrussia.ru/
* https://v2.slashlib.me/
* https://wattpad.com/
* https://wuxiaworld.ru/
* https://younettranslate.com/
* https://ранобэ.рф/
</pre>
</details>

Последняя версия доступна по [ссылке](https://github.com/OnlyFart/Elib2Ebook/releases/latest)

Portable версии запускаются без установленного NET Runtime. За настройку сборки Portalbe версий большая благодарность [@alfeg](https://github.com/alfeg)

Для запуска не Portable версии необходим установленный NET Runtime версии 8 или выше, который можно скачать с сайта Microsoft [здесь (на английском)](https://dotnet.microsoft.com/en-us/download/dotnet/7.0)

## Пример вызова
```
Elib2Ebook.exe -u https://author.today/work/212721 -f epub
```

## Пример вызова для генерации книги в нескольких форматах
```
Elib2Ebook.exe -u https://author.today/work/212721 -f epub,fb2
```

## Пример вызова для генерации книги с указанием начальной главы 
```
Elib2Ebook.exe -u https://author.today/work/212721 -f epub,fb2 --start 3
```

## Пример вызова для генерации книги с указанием конечной главы 
```
Elib2Ebook.exe -u https://author.today/work/212721 -f epub,fb2 --end 10
```

## Пример вызова для генерации книги с указанием начальной и конечной главы
```
Elib2Ebook.exe -u https://author.today/work/212721 -f epub,fb2 --start 3 --end 10
```

## Пример вызова для генерации книги с указанием индекса начиная с конца (в книге будут 3 последних главы)
```
Elib2Ebook.exe -u https://author.today/work/212721 -f epub,fb2 --start -3
```

## Пример вызова для генерации книги с указанием индекса начиная с конца (в книге будет только предпоследняя глава)
```
Elib2Ebook.exe -u https://author.today/work/212721 -f epub,fb2 --start -2 --end -1
```

## Пример вызова c указанием логина и пароля для скачивания платных книг
```
Elib2Ebook.exe -u https://author.today/work/212721 -f epub,fb2 -l vasya -p pupkin
```

## Полный список опций 
| Команда                | Описание                                                                                  |
|------------------------|-------------------------------------------------------------------------------------------|
| -u, --url              | Обязательное. Ссылка на книгу                                                             |
| -f, --format           | Обязательное. Формат для сохранения книги. Допустимые значения: epub, fb2, cbz, json, txt |
| -l, --login            | Логин от системы                                                                          |
| -p, --password         | Пароль от системы                                                                         |
| --proxy                | Прокси в формате (http or socks4 or socks5)://host:port/                                  |
| -s, --save             | Директория для сохранения книги                                                           |
| -c, --cover            | Сохранить обложку книги в отдельный файл                                                  |
| -t, --timeout          | (По-умолчанию: 5) Timeout для запросов в секундах                                         |
| --no-image             | Не загружать картинки                                                                     |
| --temp                 | Директория для хранения временных файлов                                                  |
| --save-temp            | Не удалять временную директорию                                                           |
| --start                | Стартовый номер главы                                                                     |
| --end                  | Конечный номер главы                                                                      |
