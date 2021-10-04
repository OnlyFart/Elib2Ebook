namespace Author.Today.Epub.Converter.Exceptions {
    public class BookNotFoundException : BookException {
        public BookNotFoundException(string bookId) : base("Книга и идентификатором {0} не существует.", bookId) {
            
        }
    }
}