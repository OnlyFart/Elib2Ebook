using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Core.Configs;
using Core.Extensions;
using Core.Misc;
using Core.Types.Book;
using Microsoft.Extensions.Logging;

namespace Core.Logic.Builders;

public class AdditionaFileBuilder  {
    private readonly Options _options;
    private readonly ILogger _logger;

    public AdditionaFileBuilder(Options options, ILogger logger) {
        _options = options;
        _logger = logger;
    }

    private void CreateDirectory(string directory) {
        if (!Directory.Exists(directory)) {
            Directory.CreateDirectory(directory);
        }
    }

    public async Task Build(Book book) {
        var additionalPath = $"{book.Author.Name} - {book.Title}".Crop(100).RemoveInvalidChars();
        if (!string.IsNullOrWhiteSpace(_options.SavePath)) {
            additionalPath = Path.Combine(_options.SavePath, additionalPath);
        }

        CreateDirectory(additionalPath);

        if (_options.HasAdditionalType(AdditionalTypeEnum.Image)) {
            _logger.LogInformation("Начинаю сохранение изображений из книги");
            foreach (var chapter in book.Chapters ?? []) {
                if (!chapter.Images.Any()) {
                    _logger.LogInformation($"В главе {chapter.Title.CoverQuotes()} нет изображений");
                    continue;
                }

                var subPath = Path.Combine(additionalPath, AdditionalFileCollection.IMAGES_KEY, chapter.Title.RemoveInvalidChars());
                CreateDirectory(subPath);

                _logger.LogInformation($"Начинаю сохранение изображений из части {chapter.Title.CoverQuotes()}");
                var c = 0;
                foreach (var image in chapter.Images) {
                    var fileName = Path.Combine(subPath, $"{++c}{Path.GetExtension(image.FullName)}");
                    await File.WriteAllBytesAsync(fileName, image.Content);
                }

                _logger.LogInformation($"Cохранение изображений из части {chapter.Title.CoverQuotes()} завершено");
            }

            _logger.LogInformation("Сохранение изображений из книги завешено");

            if (book.Cover != default) {
                var subPath = Path.Combine(additionalPath, AdditionalFileCollection.IMAGES_KEY);
                CreateDirectory(subPath);

                var fileName = Path.Combine(subPath, $"Cover{Path.GetExtension(book.Cover.FullName)}");
                await File.WriteAllBytesAsync(fileName, book.Cover.Content);
            }
        }

        if (book.AdditionalFiles.Collection.Count == 0) {
            _logger.LogInformation("Нет дополнительных файлов");
            return;
        }
        
        foreach (var files in book.AdditionalFiles.Collection) {
            var subPath = Path.Combine(additionalPath, files.Key);
            CreateDirectory(subPath);
            
            foreach (var file in files.Value) {
                var fileName = Path.Combine(subPath, file.FullName);
                _logger.LogInformation($"Начинаю сохранение дополнительного файла {fileName.CoverQuotes()}");
                await File.WriteAllBytesAsync(fileName, file.Content);
                _logger.LogInformation($"Cохранение дополнительного файла {fileName.CoverQuotes()} завершено");
            }
        }
    }
}