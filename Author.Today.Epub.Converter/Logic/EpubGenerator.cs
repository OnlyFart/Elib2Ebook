using System;
using System.IO;
using System.Linq;
using Author.Today.Epub.Converter.Extensions;
using Author.Today.Epub.Converter.Types.Book;
using EpubSharp;

namespace Author.Today.Epub.Converter.Logic {
    public class EpubGenerator {
        private readonly string _savePath;

        public EpubGenerator(string savePath) {
            _savePath = savePath;
        }
        
        public void Generate(BookMeta book) {
            var writer = new EpubWriter();
            writer.AddAuthor(book.AuthorName);
            writer.SetTitle(book.Title);

            if (book.Cover != null) {
                writer.SetCover(book.Cover.Content, book.Cover.Format);
            }

            foreach (var chapter in book.Chapters.Where(c => c.IsValid)) {
                foreach (var image in chapter.Images) {
                    writer.AddFile(image.Path, image.Content, image.Format.ToEpubContentType());
                }

                writer.AddChapter(chapter.Title, chapter.Content);
            }

            var fileName = $"{book.Title}.epub";
            if (!string.IsNullOrWhiteSpace(_savePath)) {
                if (!Directory.Exists(_savePath)) {
                    Directory.CreateDirectory(_savePath);
                }
                
                fileName = Path.Combine(_savePath, fileName);
            }
            
            writer.Write(fileName);
            Console.WriteLine($"Книга {fileName} успешно сохранена");
        }
    }
}
