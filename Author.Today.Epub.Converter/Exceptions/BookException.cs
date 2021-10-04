using System;
using Author.Today.Epub.Converter.Extensions;

namespace Author.Today.Epub.Converter.Exceptions {
    public class BookException : Exception {
        public BookException(string pattern, string bookId) : base(string.Format(pattern, bookId.CoverQuotes())){
            
        }
    }
}