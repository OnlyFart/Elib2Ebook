using System;
using System.IO;
using System.Linq;
using Author.Today.Epub.Converter.Configs;
using Author.Today.Epub.Converter.Extensions;
using Author.Today.Epub.Converter.Types.Book;
using EpubSharp;
using EpubSharp.Format;

namespace Author.Today.Epub.Converter.Logic {
    public class EpubGenerator {
        private readonly EpubGeneratorConfig _config;

        public EpubGenerator(EpubGeneratorConfig config){
            _config = config;
        }
        
        public void Generate(BookMeta book) {
            var writer = new EpubWriter();
            writer.AddAuthor(book.Author);
            writer.SetTitle(book.Title);

            if (book.Cover != null) {
                writer.SetCover(book.Cover.Content, book.Cover.Format);
            }

            foreach (var file in Directory.GetFiles(_config.PatternsPath, "*.ttf")) {
                Console.WriteLine($"Добавляем шрифт {file}");
                writer.AddFile(Path.GetFileName(file), File.ReadAllBytes(file), EpubContentType.FontTruetype);
            }
            
            foreach (var file in Directory.GetFiles(_config.PatternsPath, "*.css")) {
                Console.WriteLine($"Добавляем таблицу стилей {file}");
                writer.AddFile(Path.GetFileName(file), File.ReadAllBytes(file), EpubContentType.Css);
            }

            foreach (var chapter in book.Chapters.Where(c => c.IsValid)) {
                foreach (var image in chapter.Images) {
                    writer.AddFile(image.Path, image.Content, image.Format.ToEpubContentType());
                }

                Console.WriteLine($"Добавляем часть {chapter.Title}");
                writer.AddChapter(chapter.Title, chapter.Content);
            }

            var fileName = $"{book.Title}.epub";
            if (!string.IsNullOrWhiteSpace(_config.SavePath)) {
                if (!Directory.Exists(_config.SavePath)) {
                    Directory.CreateDirectory(_config.SavePath);
                }
                
                fileName = Path.Combine(_config.SavePath, fileName);
            }
            
            writer.Write(fileName);
            Console.WriteLine($"Книга {fileName} успешно сохранена");
        }
    }
}
