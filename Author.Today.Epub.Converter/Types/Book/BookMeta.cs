using System.Collections.Generic;

namespace Author.Today.Epub.Converter.Types.Book {
    public class BookMeta {
        /// <summary>
        /// Идентификатор книги
        /// </summary>
        public long Id;
        
        /// <summary>
        /// Название книги
        /// </summary>
        public string Title { get; set; }
        
        /// <summary>
        /// Автор книги
        /// </summary>
        public string Author { get; set; }
        
        /// <summary>
        /// Обложка
        /// </summary>
        public Image Cover;
        
        /// <summary>
        /// Части
        /// </summary>
        public IEnumerable<Chapter> Chapters { get; set; }

        public BookMeta(long id) {
            Id = id;
        }
    }
}
