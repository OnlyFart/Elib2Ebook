using System;
using OnlineLib2Ebook.Extensions;

namespace OnlineLib2Ebook.Exceptions {
    public class BookException : Exception {
        public BookException(string pattern, string bookId) : base(string.Format(pattern, bookId.CoverQuotes())){
            
        }
    }
}