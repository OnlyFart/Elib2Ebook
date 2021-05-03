using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Author.Today.Epub.Converter.Extensions;
using Author.Today.Epub.Converter.Types.Book;
using EpubSharp;
using EpubSharp.Format;

namespace Author.Today.Epub.Converter.Logic {
    public class EpubBuilder {
        private readonly EpubWriter _writer;

        private EpubBuilder() {
            _writer = new EpubWriter();
        }

        /// <summary>
        /// Создание нового объекта Builder'a
        /// </summary>
        /// <returns></returns>
        public static EpubBuilder Create() {
            return new();
        }

        /// <summary>
        /// Добавление автора книги
        /// </summary>
        /// <param name="author">Автор</param>
        /// <returns></returns>
        public EpubBuilder AddAuthor(string author) {
            _writer.AddAuthor(author);
            return this;
        }

        /// <summary>
        /// Указание названия книги
        /// </summary>
        /// <param name="title">Название книги</param>
        /// <returns></returns>
        public EpubBuilder WithTitle(string title) {
            _writer.SetTitle(title);
            return this;
        }

        /// <summary>
        /// Добавление обложки книги
        /// </summary>
        /// <param name="cover">Обложка</param>
        /// <returns></returns>
        public EpubBuilder WithCover(Image cover) {
            if (cover != null) {
                _writer.SetCover(cover.Content, cover.Format);
            }

            return this;
        }

        /// <summary>
        /// Добавление внешних файлов
        /// </summary>
        /// <param name="directory">Путь к директории с файлами</param>
        /// <param name="searchPattern">Шаблон поиска файлов</param>
        /// <param name="type">Тип файла</param>
        /// <returns></returns>
        public EpubBuilder WithFiles(string directory, string searchPattern, EpubContentType type) {
            foreach (var file in Directory.GetFiles(directory, searchPattern)) {
                Console.WriteLine($"Добавляем файл {file.CoverQuotes()}");
                _writer.AddFile(Path.GetFileName(file), File.ReadAllBytes(file), type);
            }

            return this;
        }

        /// <summary>
        /// Добавление списка частей книги
        /// </summary>
        /// <param name="chapters">Список частей</param>
        /// <returns></returns>
        public EpubBuilder WithChapters(IEnumerable<Chapter> chapters) {
            foreach (var chapter in chapters.Where(c => c.IsValid)) {
                foreach (var image in chapter.Images) {
                    _writer.AddFile(image.Path, image.Content, image.Format.ToEpubContentType());
                }

                Console.WriteLine($"Добавляем часть {chapter.Title.CoverQuotes()}");
                _writer.AddChapter(chapter.Title, chapter.Content);
            }

            return this;
        }

        /// <summary>
        ///  Создание epub файла
        /// </summary>
        /// <param name="directory">Директоия для сохранения</param>
        /// <param name="name">Имя файла</param>
        public void Build(string directory, string name) {
            var fileName = $"{name}.epub".RemoveInvalidChars();

            if (!string.IsNullOrWhiteSpace(directory)) {
                if (!Directory.Exists(directory)) {
                    Directory.CreateDirectory(directory);
                }

                fileName = Path.Combine(directory, fileName);
            }

            _writer.Write(fileName);
            Console.WriteLine($"Книга {fileName.CoverQuotes()} успешно сохранена");
        }
    }
}