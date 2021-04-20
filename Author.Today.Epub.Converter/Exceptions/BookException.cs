using System;
using Author.Today.Epub.Converter.Extensions;

namespace Author.Today.Epub.Converter.Exceptions {
    public class BookException : Exception {
        public BookException(string pattern, long bookId) : base(string.Format(pattern, bookId.ToString().CoverQuotes())){
            
        }
    }
}