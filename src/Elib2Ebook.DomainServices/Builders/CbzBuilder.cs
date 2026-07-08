using System.IO.Compression;
using Elib2Ebook.Domain.Book;
using Elib2Ebook.DomainServices.Configs;
using Microsoft.Extensions.Logging;

namespace Elib2Ebook.DomainServices.Builders;

public class CbzBuilder(Options options, ILogger logger) : BuilderBase(options, logger)
{
    protected override string Extension => "cbz";

    protected override async Task BuildInternal(Book book, string fileName)
    {
        if (File.Exists(fileName))
        {
            File.Delete(fileName);
        }

        using var archive = ZipFile.Open(fileName, ZipArchiveMode.Create);

        var c = 0;
        foreach (var chapter in book.Chapters)
        {
            if (chapter.Images == null)
            {
                continue;
            }

            foreach (var image in chapter.Images)
            {
                var entry = archive.CreateEntry($"{++c}{image.Extension}", CompressionLevel.Optimal);
                await using var entryStream = entry.Open();
                await using var fileStream = image.GetStream();
                await fileStream.CopyToAsync(entryStream);
            }
        }
    }
}
