using System.IO;
using System.Threading.Tasks;
using Core.Configs;
using Core.Extensions;
using Core.Types.Book;
using Microsoft.Extensions.Logging;

namespace Core.Logic.Builders; 

public abstract class BuilderBase {
    protected readonly Options Options;
    protected readonly ILogger Logger;

    protected abstract string Extension { get;}

    protected BuilderBase(Options options, ILogger logger) {
        Options = options;
        Logger = logger;
    }

    /// <summary>
    ///  Создание файла
    /// </summary>
    /// <param name="book">Книга</param>
    /// <param name="fileName">Имя файла</param>
    protected abstract Task BuildInternal(Book book, string fileName);

    /// <summary>
    /// Получение имени файла
    /// </summary>
    /// <param name="book"></param>
    /// <returns></returns>
    protected virtual string GetFileName(Book book) => $"{GetTitle(book)}.{Extension}".RemoveInvalidChars();
    
    /// <summary>
    /// Получение полного названия книги
    /// </summary>
    /// <param name="book"></param>
    /// <returns></returns>
    protected virtual string GetTitle(Book book) => $"{book.Author.Name} - {book.Title}".Crop(100);

    
    protected virtual bool PreCheck(Book book) => true;

    /// <summary>
    ///  Создание  файла
    /// </summary>
    /// <param name="book">Книга</param>
    public async Task Build(Book book) {
        if (!PreCheck(book)) {
            return;
        }
        
        var fileName = GetFileName(book);
        
        if (!string.IsNullOrWhiteSpace(Options.SavePath)) {
            if (!Directory.Exists(Options.SavePath)) {
                Directory.CreateDirectory(Options.SavePath);
            }

            fileName = Path.Combine(Options.SavePath, fileName);
        }
        
        Logger.LogInformation($"Начинаю сохранение книги {fileName.CoverQuotes()}");
        await BuildInternal(book, fileName);
        
        if (Options.Cover) {
            await SaveCover(Options.SavePath, book.Cover, GetTitle(book));
        }
        
        Logger.LogInformation($"Книга {fileName.CoverQuotes()} успешно сохранена");
    }

    /// <summary>
    /// Сохранение обложки книни в отдельный файл
    /// </summary>
    /// <param name="directory"></param>
    /// <param name="cover"></param>
    /// <param name="name"></param>
    private async Task SaveCover(string directory, Image cover, string name) {
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