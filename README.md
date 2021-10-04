# Author.Today.Epub.Converter
Инструмент для сохранения любой доступной книги с со следующих сайтов в форматах epub или fb2:
* https://author.today/
* https://litnet.com/
* https://litmarket.ru/
* https://readli.net/

## Пример вызова сервиса
```
Author.Today.Epub.Converter.exe --url https://litnet.com/ru/book/kniga-7-vladyka-magii-b364041 --format epub
```

## Где 
```
--url - ссылка на книгу https://author.today/
--format - формат для сохранения
```

## Полный список опций 

```
Author.Today.Epub.Converter.exe --help
```

## Публикация
```
dotnet publish -c Release -o Binary/win-x64 -r win-x64 --self-contained true
dotnet publish -c Release -o Binary/linux-x64 -r linux-x64 --self-contained true
```
