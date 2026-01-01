using System.IO;
using System.Threading.Tasks;
using Core.Configs;
using Core.Extensions;
using Core.Types.Book;
using Core.Types.Common;
using Microsoft.Extensions.Logging;

namespace Core.Logic.Builders; 

public abstract class BuilderBase(Options options, ILogger logger) {
    protected readonly Options Options = options;
    protected readonly ILogger Logger = logger;

    protected abstract string Extension { get;}

    /// <summary>
    ///  Создание файла
    /// </summary>
    /// <param name="book">Книга</param>
    /// <param name="fileName">Имя файла</param>
    protected abstract Task BuildInternal(Book book, string fileName);
    protected virtual async Task SplitBuild(Book book, string fileName) { await BuildInternal(book, fileName); }

    /// <summary>
    /// Получение имени файла
    /// </summary>
    /// <param name="book"></param>
    /// <returns></returns>
    protected virtual string GetFileName(Book book) => $"{GetTitle(book)}.{Extension}";
    
    /// <summary>
    /// Получение полного названия книги
    /// </summary>
    /// <param name="book"></param>
    /// <returns></returns>
    protected virtual string GetTitle(Book book) => BookNameBuilder.Build(Options.BookNamePattern, book).Crop(100);

    /// <summary>
    ///  Создание  файла
    /// </summary>
    /// <param name="book">Книга</param>
    public async Task Build(Book book) {
        var fileName = GetFileName(book);

        if (book.SupportSplitting && (Options.SplitChapters || Options.SplitVolumes))
        {
            fileName = GetTitle(book);
        }
        
        if (!string.IsNullOrWhiteSpace(Options.SavePath)) {
            if (!Directory.Exists(Options.SavePath)) {
                Directory.CreateDirectory(Options.SavePath);
            }

            fileName = Path.Combine(Options.SavePath, fileName);
        }
        
        Logger.LogInformation($"Начинаю сохранение книги {fileName.CoverQuotes()}");
        if (book.SupportSplitting && (Options.SplitChapters || Options.SplitVolumes)) {
            await SplitBuild(book, fileName);
        } else {
            await BuildInternal(book, fileName);
        }
        
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
    private async Task SaveCover(string directory, TempFile cover, string name) {
        if (cover == null) {
            return;
        }
        
        var fileName = $"{name}_cover{cover.Extension}";

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
