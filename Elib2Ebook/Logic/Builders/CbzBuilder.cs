using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Elib2Ebook.Configs;
using Elib2Ebook.Types.Book;

namespace Elib2Ebook.Logic.Builders; 

public class CbzBuilder : BuilderBase {
    public CbzBuilder(Options options) : base(options) {
        
    }

    protected override string Extension => "cbz";

    protected override async Task BuildInternal(Book book, string fileName) {
        if (File.Exists(fileName)) {
            File.Delete(fileName);
        }
        
        using var archive = ZipFile.Open(fileName, ZipArchiveMode.Create);
        
        var c = 0;
        foreach (var chapter in book.Chapters) {
            if (chapter.Images == default) {
                continue;
            }

            foreach (var image in chapter.Images) {
                var entry = archive.CreateEntry($"{++c}.{image.Extension}", CompressionLevel.Optimal);
                await using var entryStream = entry.Open();
                await using var fileStream = image.GetStream();
                await fileStream.CopyToAsync(entryStream);
            }
        }
    }
}