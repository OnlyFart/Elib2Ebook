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
        private EpubBuilder(){
            _writer = new EpubWriter();
        }

        public static EpubBuilder Create(){
            return new();
        }

        public EpubBuilder AddAuthor(string author){
            _writer.AddAuthor(author);
            return this;
        }

        public EpubBuilder WithTitle(string title){
            _writer.SetTitle(title);
            return this;
        }

        public EpubBuilder WithCover(Image cover){
            if (cover != null) {
                _writer.SetCover(cover.Content, cover.Format);
            }

            return this;
        }

        public EpubBuilder WithFiles(string path, string searchPattern, EpubContentType type) {
            foreach (var file in Directory.GetFiles(path, searchPattern)) {
                Console.WriteLine($"Добавляем файл {file}");
                _writer.AddFile(Path.GetFileName(file), File.ReadAllBytes(file), EpubContentType.FontTruetype);
            }

            return this;
        }

        public EpubBuilder WithChapters(IEnumerable<Chapter> chapters){
            foreach (var chapter in chapters.Where(c => c.IsValid)) {
                foreach (var image in chapter.Images) {
                    _writer.AddFile(image.Path, image.Content, image.Format.ToEpubContentType());
                }

                Console.WriteLine($"Добавляем часть {chapter.Title}");
                _writer.AddChapter(chapter.Title, chapter.Content);
            }

            return this;
        }

        public void Build(string path, string name){
            var fileName = $"{name}.epub".RemoveInvalidChars();

            if (!string.IsNullOrWhiteSpace(path)) {
                if (!Directory.Exists(path)) {
                    Directory.CreateDirectory(path);
                }
                
                fileName = Path.Combine(path, fileName);
            }
            
            _writer.Write(fileName);
            Console.WriteLine($"Книга {fileName} успешно сохранена");
        }
    }
}