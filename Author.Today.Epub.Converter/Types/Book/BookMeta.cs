using System.Collections.Generic;

namespace Author.Today.Epub.Converter.Types.Book {
    public class BookMeta {
        public long Id;
        public string Title { get; set; }
        public string AuthorName { get; set; }
        public Image Cover;
        public List<Chapter> Chapters { get; set; }

        public BookMeta(long id) {
            Id = id;
        }
    }
}
