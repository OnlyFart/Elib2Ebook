using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Elib2Ebook.Extensions;
using Elib2Ebook.Types.Book;

namespace Elib2Ebook.Logic.Builders; 

public abstract class BuilderBase
{
    /// <summary>
    /// Добавление автора книги
    /// </summary>
    /// <param name="author">Автор</param>
    /// <returns></returns>
    public abstract BuilderBase AddAuthor(Author author);

    /// <summary>
    /// Указание названия книги
    /// </summary>
    /// <param name="title">Название книги</param>
    /// <returns></returns>
    public abstract BuilderBase WithTitle(string title);

    /// <summary>
    /// Добавление обложки книги
    /// </summary>
    /// <param name="cover">Обложка</param>
    /// <returns></returns>
    public abstract BuilderBase WithCover(Image cover);
    
    /// <summary>
    /// Добавление адреса книги
    /// </summary>
    /// <param name="url">Url</param>
    /// <returns></returns>
    public abstract BuilderBase WithBookUrl(Uri url);
    
    /// <summary>
    /// Добавление описания книги
    /// </summary>
    /// <param name="annotation">Описание</param>
    /// <returns></returns>
    public abstract BuilderBase WithAnnotation(string annotation);

    /// <summary>
    /// Добавление внешних файлов
    /// </summary>
    /// <param name="directory">Путь к директории с файлами</param>
    /// <param name="searchPattern">Шаблон поиска файлов</param>
    /// <param name="type">Тип файла</param>
    /// <returns></returns>
    public abstract BuilderBase WithFiles(string directory, string searchPattern);

    /// <summary>
    /// Добавление списка частей книги
    /// </summary>
    /// <param name="chapters">Список частей</param>
    /// <returns></returns>
    public abstract BuilderBase WithChapters(IEnumerable<Chapter> chapters);

    /// <summary>
    /// Добавление цикла книги
    /// </summary>
    /// <param name="seria"></param>
    /// <returns></returns>
    public abstract BuilderBase WithSeria(Seria seria);

    /// <summary>
    /// Доавления языка книги
    /// </summary>
    /// <param name="lang">Язык</param>
    /// <returns></returns>
    public abstract BuilderBase WithLang(string lang);

    /// <summary>
    ///  Создание файла
    /// </summary>
    /// <param name="name">Имя файла</param>
    protected abstract Task BuildInternal(string name);

    /// <summary>
    /// Получение имени файла
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    protected abstract string GetFileName(string name);

    /// <summary>
    ///  Создание  файла
    /// </summary>
    /// <param name="directory">Директоия для сохранения</param>
    /// <param name="name">Имя файла</param>
    public async Task Build(string directory, string name) {
        var fileName = GetFileName(name);
            
        if (!string.IsNullOrWhiteSpace(directory)) {
            if (!Directory.Exists(directory)) {
                Directory.CreateDirectory(directory);
            }

            fileName = Path.Combine(directory, fileName);
        }
        
        Console.WriteLine($"Начинаю сохранение книги {fileName.CoverQuotes()}");
            
        await BuildInternal(fileName);

        Console.WriteLine($"Книга {fileName.CoverQuotes()} успешно сохранена");
    }

    /// <summary>
    /// Сохранение обложки книни в отдельный файл
    /// </summary>
    /// <param name="directory"></param>
    /// <param name="cover"></param>
    /// <param name="name"></param>
    public async Task SaveCover(string directory, Image cover, string name) {
        if (cover == null) {
            return;
        }
        
        var fileName = $"{name}_cover.{cover.Extension}".RemoveInvalidChars();

        if (!string.IsNullOrWhiteSpace(directory)) {
            if (!Directory.Exists(directory)) {
                Directory.CreateDirectory(directory);
            }

            fileName = Path.Combine(directory, fileName);
        }

        await using var file = File.OpenWrite(fileName);
        await using var coverStream = cover.GetStream();
        await coverStream.CopyToAsync(file);
    }
}