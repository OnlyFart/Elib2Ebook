# Author.Today.Epub.Converter
Инструмент для сохрания любой доступной книги с сайта https://author.today/ в формате epub

* [.net 5](https://dotnet.microsoft.com/download/dotnet/5.0) 

## Пример вызова сервиса
```
Author.Today.Epub.Converter.exe --id 123498
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
