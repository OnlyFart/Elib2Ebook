namespace OnlineLib2Ebook.Exceptions {
    public class BookForbiddenException : BookException {
        public BookForbiddenException(string bookId) : base("Книга и идентификатором {0} недоступна.", bookId){
            
        }
    }
}