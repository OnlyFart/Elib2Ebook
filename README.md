# OnlineLib2Ebook
Инструмент для сохранения любой доступной книги со следующих сайтов в форматах epub или fb2:
* https://author.today/
* https://dark-novels.ru/
* https://jaomix.ru/
* https://litmarket.ru/
* https://litnet.com/
* https://ranobes.com/
* https://readli.net/
* http://samlib.ru/
* https://tl.rulate.ru/
* https://ранобэ.рф/

## Пример вызова сервиса
```
OnlineLib2Ebook.exe --url https://litnet.com/ru/book/kniga-7-vladyka-magii-b364041 --format epub
```

## Где 
```
--url - ссылка на книгу
--format - формат для сохранения
```

## Полный список опций 

```
OnlineLib2Ebook.exe --help
```

## Публикация
```
dotnet publish -c Release -o Binary/win-x64 -r win-x64 --self-contained true
dotnet publish -c Release -o Binary/linux-x64 -r linux-x64 --self-contained true
```
