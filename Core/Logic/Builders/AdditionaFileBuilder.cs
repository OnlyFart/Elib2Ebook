using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Core.Configs;
using Core.Extensions;
using Core.Misc;
using Core.Types.Book;
using Microsoft.Extensions.Logging;

namespace Core.Logic.Builders;

public class AdditionaFileBuilder(Options options, ILogger logger) {
    private void CreateDirectory(string directory) {
        if (!Directory.Exists(directory)) {
            Directory.CreateDirectory(directory);
        }
    }

    public async Task Build(Book book) {
        var additionalPath = BookNameBuilder.Build(options.BookNamePattern, book);
        if (!string.IsNullOrWhiteSpace(options.SavePath)) {
            additionalPath = Path.Combine(options.SavePath, additionalPath);
        }

        CreateDirectory(additionalPath);

        if (options.HasAdditionalType(AdditionalTypeEnum.Images)) {
            logger.LogInformation("Начинаю сохранение изображений из книги");
            foreach (var chapter in book.Chapters ?? []) {
                if (!chapter.Images.Any()) {
                    logger.LogInformation($"В главе {chapter.Title.CoverQuotes()} нет изображений");
                    continue;
                }

                var subPath = Path.Combine(additionalPath, AdditionalTypeEnum.Images.ToString(), chapter.Title.RemoveInvalidChars());
                CreateDirectory(subPath);

                logger.LogInformation($"Начинаю сохранение изображений из части {chapter.Title.CoverQuotes()}");
                var c = 0;
                foreach (var image in chapter.Images) {
                    var fileName = Path.Combine(subPath, $"{++c}{Path.GetExtension(image.FullName)}".RemoveInvalidChars());
                    await File.WriteAllBytesAsync(fileName, image.Content);
                }

                logger.LogInformation($"Cохранение изображений из части {chapter.Title.CoverQuotes()} завершено");
            }

            logger.LogInformation("Сохранение изображений из книги завешено");

            if (book.Cover != default) {
                var subPath = Path.Combine(additionalPath, AdditionalTypeEnum.Images.ToString());
                CreateDirectory(subPath);

                var fileName = Path.Combine(subPath, $"Cover{Path.GetExtension(book.Cover.FullName)}".RemoveInvalidChars());
                await File.WriteAllBytesAsync(fileName, book.Cover.Content);
            }
        }

        if (book.AdditionalFiles.Collection.Count == 0) {
            logger.LogInformation("Нет дополнительных файлов");
            return;
        }
        
        foreach (var files in book.AdditionalFiles.Collection) {
            var subPath = Path.Combine(additionalPath, files.Key.ToString());
            CreateDirectory(subPath);
            
            foreach (var file in files.Value) {
                var fileName = Path.Combine(subPath, file.FullName.RemoveInvalidChars());
                logger.LogInformation($"Начинаю сохранение дополнительного файла {fileName.CoverQuotes()}");
                await File.WriteAllBytesAsync(fileName, file.Content);
                logger.LogInformation($"Cохранение дополнительного файла {fileName.CoverQuotes()} завершено");
            }
        }
    }
}