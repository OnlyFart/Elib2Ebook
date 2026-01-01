using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Core.Configs;
using Core.Types.Book;
using Microsoft.Extensions.Logging;

namespace Core.Logic.Builders;

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
            if (chapter.Images == default)
            {
                continue;
            }

            foreach (var image in chapter.Images)
            {
                var image_name = $"{chapter.VolumeNumber}-{chapter.ChapterNumber}-{++c}{image.Extension}";
                var entry = archive.CreateEntry(image_name, CompressionLevel.Optimal);
                await using var entryStream = entry.Open();
                await using var fileStream = image.GetStream();
                await fileStream.CopyToAsync(entryStream);
            }
        }
    }

    protected override async Task SplitBuild(Book book, string fileName)
    {
        if (options.SplitVolumes && options.SplitChapters)
        {
            foreach (var volumeNumber in book.Volumes)
            {
                var volume_name = $"{fileName}.v{volumeNumber}.{Extension}";
                using var volume_archive = ZipFile.Open(volume_name, ZipArchiveMode.Create);

                foreach (var chapter in book.Chapters.Where(c => c.VolumeNumber == volumeNumber))
                {
                    if (chapter.Images == default)
                    {
                        continue;
                    }

                    var chapter_name = $"{fileName}.v{chapter.VolumeNumber}.c{chapter.ChapterNumber}.{Extension}";

                    using var chapter_archive = ZipFile.Open(chapter_name, ZipArchiveMode.Create);

                    var c = 0;
                    foreach (var image in chapter.Images)
                    {
                        var image_name = $"{chapter.VolumeNumber}-{chapter.ChapterNumber}-{++c}{image.Extension}";
                        var volume_entry = volume_archive.CreateEntry(image_name, CompressionLevel.Optimal);
                        var chapter_entry = chapter_archive.CreateEntry(image_name, CompressionLevel.Optimal);
                        await using var volume_entryStream = volume_entry.Open();
                        await using var chapter_entryStream = chapter_entry.Open();
                        await using var volume_fileStream = image.GetStream();
                        await using var chapter_fileStream = image.GetStream();
                        await volume_fileStream.CopyToAsync(volume_entryStream);
                        await chapter_fileStream.CopyToAsync(chapter_entryStream);
                    }
                    Logger.LogInformation($"Книга {chapter_name} успешно сохранена");
                    chapter_archive.Dispose();
                }
                Logger.LogInformation($"Книга {volume_name} успешно сохранена");
                volume_archive.Dispose();
            }
        }
        else if (options.SplitVolumes)
        {
            foreach (var volumeNumber in book.Volumes)
            {
                var volume_name = $"{fileName}.v{volumeNumber}.{Extension}";

                using var volume_archive = ZipFile.Open(volume_name, ZipArchiveMode.Create);

                var c = 0;
                foreach (var chapter in book.Chapters.Where(c => c.VolumeNumber == volumeNumber))
                {
                    if (chapter.Images == default)
                    {
                        continue;
                    }
                    foreach (var image in chapter.Images)
                    {
                        var image_name = $"{chapter.VolumeNumber}-{chapter.ChapterNumber}-{++c}{image.Extension}";
                        var volume_entry = volume_archive.CreateEntry(image_name, CompressionLevel.Optimal);
                        await using var volume_entryStream = volume_entry.Open();
                        await using var fileStream = image.GetStream();
                        await fileStream.CopyToAsync(volume_entryStream);
                    }
                }
                Logger.LogInformation($"Книга {volume_name} успешно сохранена");
                volume_archive.Dispose();
            }
        }
        else if (options.SplitChapters)
        {
            foreach (var chapter in book.Chapters)
            {
                var chapter_name = $"{fileName}.v{chapter.VolumeNumber}.c{chapter.ChapterNumber}.{Extension}";

                using var chapter_archive = ZipFile.Open(chapter_name, ZipArchiveMode.Create);
                if (chapter.Images == default)
                {
                    continue;
                }
                var c = 0;
                foreach (var image in chapter.Images)
                {
                    var image_name = $"{chapter.VolumeNumber}-{chapter.ChapterNumber}-{++c}{image.Extension}";
                    var chapter_entry = chapter_archive.CreateEntry(image_name, CompressionLevel.Optimal);
                    await using var chapter_entryStream = chapter_entry.Open();
                    await using var fileStream = image.GetStream();
                    await fileStream.CopyToAsync(chapter_entryStream);
                }
                Logger.LogInformation($"Книга {chapter_name} успешно сохранена");
                chapter_archive.Dispose();
            }
        }
        else
        {
            await BuildInternal(book, fileName);
        }
    }
}