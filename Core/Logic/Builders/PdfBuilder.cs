using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Core.Configs;
using Core.Extensions;
using Core.Misc.TempFolder;
using Core.Types.Book;
using Core.Types.Common;
using Microsoft.Extensions.Logging;
using SkiaSharp;
using WkHtmlToPdfDotNet;

namespace Core.Logic.Builders;

public class PdfBuilder : BuilderBase {
    public PdfBuilder(Options options, ILogger logger) : base(options, logger) {
    }

    protected override string Extension => "pdf";

    private static readonly SynchronizedConverter Converter = new(new PdfTools());
    
    /// <summary>
    /// Создание Xhtml документа из кода части
    /// </summary>
    /// <param name="pattern">Шаблон части</param>
    /// <param name="title">Заголовок части</param>
    /// <param name="content">Раскодированный текст</param>
    /// <returns></returns>
    private static string ApplyPattern(string pattern, string title, string content) {
        return pattern.Replace("{title}", title.CleanInvalidXmlChars().ReplaceNewLine()).Replace("{body}", content.HtmlDecode().CleanInvalidXmlChars()).AsXHtmlDoc().AsString();
    }

    /// <summary>
    /// Создание Xhtml документа из кода части
    /// </summary>
    /// <param name="pattern">Шаблон части</param>
    /// <param name="chapter"></param>
    /// <returns></returns>
    private static string GetChapter(string pattern, Chapter chapter) {
        return ApplyPattern(pattern, chapter.Title, chapter.Content);
    }

    /// <summary>
    /// Добавление внешних файлов
    /// </summary>
    /// <param name="directory">Путь к директории с файлами</param>
    /// <param name="searchPattern">Шаблон поиска файлов</param>
    /// <param name="temp"></param>
    /// <returns></returns>
    private void WithFiles(string directory, string searchPattern, TempFolder temp) {
        foreach (var file in FileProvider.Instance.GetFiles(directory, searchPattern)) {
            File.WriteAllBytes(Path.Combine(temp.Path, file.Name), file.ReadAllBytes());
        }
    }

    private async Task SaveImage(string path, TempFile file) {
        using var codec = SKCodec.Create(file.GetStream());
        if (codec.EncodedFormat == SKEncodedImageFormat.Webp) {
            using var bitmap = SKBitmap.Decode(file.Content);
            await File.WriteAllBytesAsync(Path.Combine(path, file.FullName), bitmap.Encode(SKEncodedImageFormat.Png, 100).ToArray());
        } else {
            await File.WriteAllBytesAsync(Path.Combine(path, file.FullName), file.Content);
        }
    }

    protected override async Task BuildInternal(Book book, string fileName) {
        using var tempFolder = TempFolderFactory.Create(Path.Combine(Options.TempPath ?? string.Empty, "pdf_temp"), true);
        var chapterPattern = FileProvider.Instance.ReadAllText($"{Options.ResourcesPath}/ChapterPattern.xhtml");
        
        var doc = new HtmlToPdfDocument {
            GlobalSettings = {
                PaperSize = PaperKind.A4,
                Out = fileName,
                ViewportSize = "1280x1024"
            }
        };
            
        WithFiles(Options.ResourcesPath, "*.ttf", tempFolder);
        WithFiles(Options.ResourcesPath, "*.css", tempFolder);

        foreach (var chapter in book.Chapters.Where(c => c.IsValid)) {
            var chapterPath = Path.Combine(tempFolder.Path, chapter.Title + ".html");

            await File.WriteAllTextAsync(chapterPath, GetChapter(chapterPattern, chapter));
            foreach (var img in chapter.Images) {
                await SaveImage(tempFolder.Path, img);
            }
            
            doc.Objects.Add(new ObjectSettings {
                Page = chapterPath,
                LoadSettings = new LoadSettings {
                    BlockLocalFileAccess = false,
                },
            });
        }

        Converter.Convert(doc);
    }
}