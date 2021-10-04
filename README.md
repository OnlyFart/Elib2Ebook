# Author.Today.Epub.Converter
Инструмент для сохранения любой доступной книги с сайтов https://author.today/ или https://litnet.com/ в формате epub или fb2~~~~

* [.net 5](https://dotnet.microsoft.com/download/dotnet/5.0) 

## Пример вызова сервиса
```
Author.Today.Epub.Converter.exe --url https://litnet.com/ru/book/kniga-7-vladyka-magii-b364041 --format epub
```

## Где 
```
--id - идентификатор книги на сайта https://author.today/
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
