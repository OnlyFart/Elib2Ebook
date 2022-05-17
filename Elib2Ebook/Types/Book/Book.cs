using System;
using System.Collections.Generic;
using Elib2Ebook.Configs;
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
    public Author Author { get; init; }
    
    /// <summary>
    /// Описание книги
    /// </summary>
    public string Annotation { get; set; }
        
    /// <summary>
    /// Обложка
    /// </summary>
    public Image Cover { get; init; }
    
    /// <summary>
    /// Серия
    /// </summary>
    public Seria Seria { get; set; }

    /// <summary>
    /// Части
    /// </summary>
    public IEnumerable<Chapter> Chapters { get; init; } = new List<Chapter>();

    /// <summary>
    /// Url расположения книги
    /// </summary>
    public Uri Url { get; set; }

    public Book(Uri url) {
        Url = url;
    }

    /// <summary>
    /// Сохранение книги
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="options"></param>
    /// <param name="resourcesPath">Путь к папке с ресурсами</param>
    public void Save(BuilderBase builder, Options options, string resourcesPath) {
        var title = $"{Author.Name} - {Title}".Crop(100);
        
        builder
            .AddAuthor(Author)
            .WithTitle(Title)
            .WithCover(Cover)
            .WithBookUrl(Url)
            .WithAnnotation(Annotation)
            .WithSeria(Seria)
            .WithFiles(resourcesPath, "*.ttf", EpubContentType.FontTruetype)
            .WithFiles(resourcesPath, "*.css", EpubContentType.Css)
            .WithChapters(Chapters)
            .Build(options.SavePath, title);

        if (options.Cover) {
            builder.SaveCover(options.SavePath, Cover, title);
        }
    }
}