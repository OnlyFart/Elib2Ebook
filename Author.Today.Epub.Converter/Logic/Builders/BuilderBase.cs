using System;
using System.Collections.Generic;
using System.IO;
using Author.Today.Epub.Converter.Extensions;
using Author.Today.Epub.Converter.Types.Book;
using EpubSharp.Format;

namespace Author.Today.Epub.Converter.Logic.Builders {
    public abstract class BuilderBase {
        /// <summary>
        /// Добавление автора книги
        /// </summary>
        /// <param name="author">Автор</param>
        /// <returns></returns>
        public abstract BuilderBase AddAuthor(string author);

        /// <summary>
        /// Указание названия книги
        /// </summary>
        /// <param name="title">Название книги</param>
        /// <returns></returns>
        public abstract BuilderBase WithTitle(string title);

        /// <summary>
        /// Добавление обложки книги
        /// </summary>
        /// <param name="cover">Обложка</param>
        /// <returns></returns>
        public abstract BuilderBase WithCover(Image cover);

        /// <summary>
        /// Добавление внешних файлов
        /// </summary>
        /// <param name="directory">Путь к директории с файлами</param>
        /// <param name="searchPattern">Шаблон поиска файлов</param>
        /// <param name="type">Тип файла</param>
        /// <returns></returns>
        public abstract BuilderBase WithFiles(string directory, string searchPattern, EpubContentType type);

        /// <summary>
        /// Добавление списка частей книги
        /// </summary>
        /// <param name="chapters">Список частей</param>
        /// <returns></returns>
        public abstract BuilderBase WithChapters(IEnumerable<Chapter> chapters);

        /// <summary>
        ///  Создание файла
        /// </summary>
        /// <param name="name">Имя файла</param>
        protected abstract void BuildInternal(string name);

        /// <summary>
        /// Получение имени файла
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        protected abstract string GetFileName(string name);

        /// <summary>
        ///  Создание  файла
        /// </summary>
        /// <param name="directory">Директоия для сохранения</param>
        /// <param name="name">Имя файла</param>
        public void Build(string directory, string name) {
            var fileName = GetFileName(name);
            
            if (!string.IsNullOrWhiteSpace(directory)) {
                if (!Directory.Exists(directory)) {
                    Directory.CreateDirectory(directory);
                }

                fileName = Path.Combine(directory, fileName);
            }
            
            BuildInternal(fileName);

            Console.WriteLine($"Книга {fileName.CoverQuotes()} успешно сохранена");
        }
    }
}