using Author.Today.Epub.Converter.Configs;
using Author.Today.Epub.Converter.Types.Book;
using EpubSharp.Format;

namespace Author.Today.Epub.Converter.Logic {
    public class EpubGenerator {
        private readonly EpubGeneratorConfig _config;

        public EpubGenerator(EpubGeneratorConfig config){
            _config = config;
        }
        
        public void Generate(BookMeta book){
            EpubBuilder.Create()
                .AddAuthor(book.Author)
                .WithTitle(book.Title)
                .WithCover(book.Cover)
                .WithFiles(_config.PatternsPath, "*.ttf", EpubContentType.FontTruetype)
                .WithFiles(_config.PatternsPath, "*.css", EpubContentType.Css)
                .WithChapters(book.Chapters)
                .Build(_config.SavePath, book.Title);
        }
    }
}
