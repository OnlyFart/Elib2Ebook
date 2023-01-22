using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Elib2Ebook.Configs;
using Elib2Ebook.Extensions;
using Elib2Ebook.Logic.Builders;

namespace Elib2Ebook.Types.Book; 

public class Book {
    /// <summary>
    /// Название книги
    /// </summary>
    public string Title { get; set; }
        
    /// <summary>
    /// Автор книги
    /// </summary>
    public Author Author { get; set; }
    
    /// <summary>
    /// Описание книги
    /// </summary>
    public string Annotation { get; set; }
        
    /// <summary>
    /// Обложка
    /// </summary>
    public Image Cover { get; set; }
    
    /// <summary>
    /// Серия
    /// </summary>
    public Seria Seria { get; set; }

    /// <summary>
    /// Части
    /// </summary>
    public IEnumerable<Chapter> Chapters { get; set; } = new List<Chapter>();

    /// <summary>
    /// Url расположения книги
    /// </summary>
    public Uri Url { get; set; }

    /// <summary>
    /// Язык книги
    /// </summary>
    public string Lang { get; set; } = "ru";

    public Book(Uri url) {
        Url = url;
    }

    /// <summary>
    /// Сохранение книги
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="options"></param>
    /// <param name="resourcesPath">Путь к папке с ресурсами</param>
    public async Task Save(BuilderBase builder, Options options, string resourcesPath) {
        var title = $"{Author.Name} - {Title}".Crop(100);
        
        // await builder
        //     .AddAuthor(Author)
        //     .WithTitle(Title)
        //     .WithBookUrl(Url)
        //     .WithAnnotation(Annotation)
        //     .WithCover(Cover)
        //     .WithSeria(Seria)
        //     .WithLang(Lang)
        //     .WithFiles(resourcesPath, "*.ttf")
        //     .WithFiles(resourcesPath, "*.css")
        //     .WithChapters(Chapters)
        //     .Build(options.SavePath, title);
        
        await builder
            .AddAuthor(Author)
            .WithBookUrl(Url)
            .WithTitle(Title)
            .WithAnnotation(Annotation)
            .WithCover(Cover)
            .WithLang(Lang)
            .WithSeria(Seria)
            .WithFiles(resourcesPath, "*.ttf")
            .WithFiles(resourcesPath, "*.css")
            .WithChapters(Chapters)
            .Build(options.SavePath, title);

        if (options.Cover) {
            await builder.SaveCover(options.SavePath, Cover, title);
        }
    }
}