using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Core.Configs;
using Core.Extensions;
using Core.Types.Book;
using Core.Types.Common;
using EpubSharp;
using EpubSharp.Format;
using Microsoft.Extensions.Logging;

namespace Core.Logic.Builders; 

public class EpubBuilder : BuilderBase {
    protected override string Extension => "epub";
    
    private readonly EpubWriter _writer;

    private List<TempFile> Images { get; set; } = new();

    public EpubBuilder(Options options, ILogger logger) : base(options, logger) {
        _writer = new EpubWriter();
    }

    /// <summary>
    /// Создание Xhtml документа из кода части
    /// </summary>
    /// <param name="pattern">Шаблон части</param>
    /// <param name="title">Заголовок части</param>
    /// <param name="decodeText">Раскодированный текст</param>
    /// <returns></returns>
    private static string ApplyPattern(string pattern, string title, string decodeText) {
        return pattern.Replace("{title}", title.CleanInvalidXmlChars().ReplaceNewLine()).Replace("{body}", decodeText.HtmlDecode().CleanInvalidXmlChars()).AsXHtmlDoc().AsString();
    }

    /// <summary>
    /// Добавление внешних файлов
    /// </summary>
    /// <param name="directory">Путь к директории с файлами</param>
    /// <param name="searchPattern">Шаблон поиска файлов</param>
    /// <returns></returns>
    private void WithFiles(string directory, string searchPattern) {
        foreach (var file in FileProvider.Instance.GetFiles(directory, searchPattern)) {
            var fileName = Path.GetFileName(file.Name);
            var type = file.Name.EndsWith(".ttf") ? EpubContentType.FontTruetype : 
                file.Name.EndsWith(".css") ? EpubContentType.Css : throw new Exception($"Неизвестный тип файла {fileName}");
            
            _writer.AddFile(fileName, file.ReadAllBytes(), type);
        }
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

    private void Write(Book book) {
        _writer.AddAuthor(book.Author.Name);
        
        foreach (var coAuthor in book.CoAuthors) {
            _writer.AddAuthor(coAuthor.Name);    
        }
        
        _writer.SetTitle(book.Title);
        
        if (book.Cover != null) {
            _writer.SetCover(book.Cover.Content, GetImageFormat(book.Cover.FullName));
        }
        
        if (!string.IsNullOrWhiteSpace(book.Annotation)) {
            _writer.AddDescription(book.Annotation.AsHtmlDoc().DocumentNode.GetText());    
        }
        
        if (book.Seria != default) {
            _writer.AddCollection(book.Seria.Name, book.Seria.Number);
        }

        var pattern = FileProvider.Instance.ReadAllText($"{Options.ResourcesPath}/ChapterPattern.xhtml");
        foreach (var chapter in book.Chapters.Where(c => c.IsValid)) {
            foreach (var image in chapter.Images) {
                _writer.AddFile(image.FullName, Array.Empty<byte>(), GetImageFormat(image.FullName).ToEpubContentType());
                Images.Add(image);
            }
            
            _writer.AddChapter(chapter.Title.HtmlDecode().ReplaceNewLine().RemoveInvalidChars(), ApplyPattern(pattern, chapter.Title, chapter.Content));
        }
        
        WithFiles(Options.ResourcesPath, "*.ttf");
        WithFiles(Options.ResourcesPath, "*.css");
    }

    protected override async Task BuildInternal(Book book, string fileName) {
        Write(book);
        await _writer.Write(fileName, Images.Select(image => new FileMeta(image.FullName, image.FilePath)));
    }
}