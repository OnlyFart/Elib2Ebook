using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Elib2Ebook.Extensions;
using Elib2Ebook.Types.Book;
using EpubSharp;
using EpubSharp.Format;

namespace Elib2Ebook.Logic.Builders; 

public class EpubBuilder : BuilderBase {
    private readonly EpubWriter _writer;
    private readonly string _pattern;

    private List<Image> Images { get; set; } = new();

    private EpubBuilder(string pattern) {
        _writer = new EpubWriter();
        _pattern = pattern;
    }

    /// <summary>
    /// Создание нового объекта Builder'a
    /// </summary>
    /// <returns></returns>
    public static EpubBuilder Create(string pattern) {
        return new(pattern);
    }

    /// <summary>
    /// Создание Xhtml документа из кода части
    /// </summary>
    /// <param name="title">Заголовок части</param>
    /// <param name="decodeText">Раскодированный текст</param>
    /// <returns></returns>
    private string ApplyPattern(string title, string decodeText) {
        return _pattern.Replace("{title}", title.CleanInvalidXmlChars().ReplaceNewLine()).Replace("{body}", 
            decodeText.HtmlDecode().CleanInvalidXmlChars()).AsXHtmlDoc().AsString();
    }

    /// <summary>
    /// Добавление автора книги
    /// </summary>
    /// <param name="author">Автор</param>
    /// <returns></returns>
    public override BuilderBase AddAuthor(Author author) {
        _writer.AddAuthor(author.Name);
        return this;
    }

    /// <summary>
    /// Указание названия книги
    /// </summary>
    /// <param name="title">Название книги</param>
    /// <returns></returns>
    public override BuilderBase WithTitle(string title) {
        _writer.SetTitle(title);
        return this;
    }

    /// <summary>
    /// Добавление обложки книги
    /// </summary>
    /// <param name="cover">Обложка</param>
    /// <returns></returns>
    public override BuilderBase WithCover(Image cover) {
        if (cover != null) {
            _writer.SetCover(cover.Content, GetImageFormat(cover.Name));
        }

        return this;
    }

    public override BuilderBase WithBookUrl(Uri url) {
        return this;
    }

    public override BuilderBase WithAnnotation(string annotation) {
        if (!string.IsNullOrWhiteSpace(annotation)) {
            _writer.AddDescription(annotation.AsHtmlDoc().DocumentNode.GetText());    
        }
        
        return this;
    }

    /// <summary>
    /// Добавление внешних файлов
    /// </summary>
    /// <param name="directory">Путь к директории с файлами</param>
    /// <param name="searchPattern">Шаблон поиска файлов</param>
    /// <returns></returns>
    public override BuilderBase WithFiles(string directory, string searchPattern) {
        foreach (var file in FileProvider.Instance.GetFiles(directory, searchPattern))
        {
            var fileName = Path.GetFileName(file.Name);
            var type = file.Name.EndsWith(".ttf") ? 
                EpubContentType.FontTruetype : 
                file.Name.EndsWith(".css") ? 
                    EpubContentType.Css : 
                    throw new Exception($"Неизвестный тип файла {fileName}");
            
            _writer.AddFile(fileName, file.ReadAllBytes(), type);
        }
      
        return this;
    }

    /// <summary>
    /// Добавление списка частей книги
    /// </summary>
    /// <param name="chapters">Список частей</param>
    /// <returns></returns>
    public override BuilderBase WithChapters(IEnumerable<Chapter> chapters) {
        foreach (var chapter in chapters.Where(c => c.IsValid)) {
            foreach (var image in chapter.Images) {
                _writer.AddFile(image.Name, Array.Empty<byte>(), GetImageFormat(image.Name).ToEpubContentType());
                Images.Add(image);
            }
            
            _writer.AddChapter(chapter.Title.HtmlDecode().ReplaceNewLine().RemoveInvalidChars(), ApplyPattern(chapter.Title, chapter.Content));
        }

        return this;
    }
    
    private static ImageFormat GetImageFormat(string path) {
        if (path.EndsWith(".jpg", StringComparison.InvariantCultureIgnoreCase)) {
            return ImageFormat.Jpeg;
        }

        if (path.EndsWith(".gif", StringComparison.InvariantCultureIgnoreCase)) {
            return ImageFormat.Gif;
        }

        if (path.EndsWith(".png", StringComparison.InvariantCultureIgnoreCase)) {
            return ImageFormat.Png;
        }

        return path.EndsWith(".svg", StringComparison.InvariantCultureIgnoreCase) ? ImageFormat.Svg : ImageFormat.Jpeg;
    }

    public override BuilderBase WithSeria(Seria seria) {
        if (seria != default) {
            _writer.AddCollection(seria.Name, seria.Number);
        }

        return this;
    }

    public override BuilderBase WithLang(string lang) {
        return this;
    }

    protected override async Task BuildInternal(string name) {
        await _writer.Write(name, Images.Select(image => new FileMeta(image.Name, image.FilePath)));
    }

    protected override string GetFileName(string name) {
        return $"{name}.epub".RemoveInvalidChars();
    }
}