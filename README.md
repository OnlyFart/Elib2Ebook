# Elib2Ebook
[![GitHub License](https://img.shields.io/github/license/OnlyFart/Elib2Ebook.svg?style=flat-square)](https://github.com/OnlyFart/Elib2Ebook/blob/master/LICENSE)
[![GitHub Stars](https://img.shields.io/github/stars/OnlyFart/Elib2Ebook.svg?style=flat-square)](https://github.com/OnlyFart/Elib2Ebook/stargazers)
[![GitHub forks](https://img.shields.io/github/forks/OnlyFart/Elib2Ebook.svg?style=flat-square)](https://github.com/OnlyFart/Elib2Ebook/network)
[![GitHub tag](https://img.shields.io/github/v/tag/OnlyFart/Elib2Ebook.svg?style=flat-square)](https://github.com/OnlyFart/Elib2Ebook/releases/latest)
![GitHub downloads](https://img.shields.io/github/downloads/onlyfart/elib2ebook/total?style=flat-square)



Инструмент для сохранения любой доступной книги со следующих сайтов в форматах epub, fb2, cbz:
<details>
<pre>
* https://acomics.ru/
* https://author.today/
* https://bigliba.com/
* https://bookinbook.ru/
* https://bookinist.pw/
* https://booknet.com/
* https://booknet.ua/
* https://bookstab.ru/
* https://bookriver.ru/
* https://dark-novels.ru/
* https://dreame.com/
* https://eznovels.com/
* https://fb2.top/
* https://ficbook.net/
* https://fictionbook.ru/
* https://hentailib.me/
* https://hogwartsnet.ru/
* https://hotnovelpub.com/
* https://hub-book.com/
* https://ifreedom.su/
* https://jaomix.ru/
* https://ladylib.top/
* https://lanovels.com/
* https://libbox.ru/
* https://libst.ru/
* https://lightnoveldaily.com/
* https://litexit.ru/
* https://litgorod.ru/
* https://litmarket.ru/
* https://litmir.me/
* https://litnet.com/
* https://litres.ru/
* https://manga.ovh/
* https://mangalib.me/
* https://mir-knig.com/
* https://mybook.ru/
* https://online-knigi.com.ua/
* https://noveltranslate.com/
* https://novelxo.com/
* https://prodaman.ru/
* https://ranobe-novels.ru/
* https://ranobehub.org/
* https://ranobelib.me/
* https://ranobe.ovh/
* https://ranobes.com/
* https://readli.net/
* https://readmanga.live/
* https://remanga.org/
* https://renovels.org/
* https://royalroad.com/
* https://ru.novelxo.com/
* http://samlib.ru/
* https://topliba.com/
* https://tl.rulate.ru/
* https://twilightrussia.ru/
* https://wattpad.com/
* https://wuxiaworld.ru/
* https://yaoilib.me/
* https://ранобэ.рф/
</pre>
</details>

Последняя версия доступна по [ссылке](https://github.com/OnlyFart/Elib2Ebook/releases/latest)

Для запуска не Portalbe версии необходим установленный NET Runtime версии 6 или выше, который можно скачать с сайта Microsoft [здесь (на английском)](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-desktop-6.0.10-windows-x64-installer)

Portable версии запускаются без установленного NET Runtime

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

## Пример вызова для генерации книги с укзанием индекса начиная с конца (в книге будут 3 последних главы)
```
Elib2Ebook.exe -u https://author.today/work/212721 -f epub,fb2 --start -3
```

## Пример вызова для генерации книги с укзанием индекса начиная с конца (в книге будет только предпоследняя глава)
```
Elib2Ebook.exe -u https://author.today/work/212721 -f epub,fb2 --start -2 --end -1
```

## Пример вызова c указанием логина и пароля для скачивания платных книг
```
Elib2Ebook.exe -u https://author.today/work/212721 -f epub,fb2 -l vasya -p pupkin
```

## Полный список опций 
| Команда | Описание                                                |
|----------------|--------------------------------------------------|
|  -u, --url|         Обязательное. Ссылка на книгу|
|  -f, --format|      Обязательное. Формат для сохранения книги. Допустимые значения: epub, fb2, cbz, json|
|  -l, --login|       Логин от системы|
|  -p, --password|    Пароль от системы|
|  --proxy|           Прокси в формате host:port|
|  -s, --save|        Директория для сохранения книги|
|  -c, --cover|       Сохранить обложку книги в отдельный файл|
|  -t, --timeout|     (По-умолчанию: 5) Timeout для запросов в секундах|
|  --no-image|        Не загружать картинки
|  --start|           Стартовый номер главы|
|  --end|             Конечный номер главы|
