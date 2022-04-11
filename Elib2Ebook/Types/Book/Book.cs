using System.Collections.Generic;
using Elib2Ebook.Logic.Builders;
using EpubSharp.Format;
using Elib2Ebook.Extensions;

namespace Elib2Ebook.Types.Book; 

public class Book {
    /// <summary>
    /// Название книги
    /// </summary>
    public string Title { get; init; }
        
    /// <summary>
    /// Автор книги
    /// </summary>
    public string Author { get; init; }
        
    /// <summary>
    /// Обложка
    /// </summary>
    public Image Cover { get; init; }

    /// <summary>
    /// Части
    /// </summary>
    public IEnumerable<Chapter> Chapters { get; init; }

    /// <summary>
    /// Сохранение книги
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="savePath">Путь для сохранения</param>
    /// <param name="resourcesPath">Путь к папке с ресурсами</param>
    public void Save(BuilderBase builder, string savePath, string resourcesPath) {
        builder.AddAuthor(Author)
            .WithTitle(Title)
            .WithCover(Cover)
            .WithFiles(resourcesPath, "*.ttf", EpubContentType.FontTruetype)
            .WithFiles(resourcesPath, "*.css", EpubContentType.Css)
            .WithChapters(Chapters)
            .Build(savePath, Title.Crop(100));
    }
}