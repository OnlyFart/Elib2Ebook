using System.Collections.Generic;
using Core.Extensions;
using Core.Types.Book;

namespace Core.Logic;

public class BookNameBuilder {
    public const string TITLE_PATTERN = "title";
    
    public const string AUTHOR_PATTERN = "author";
    
    public const string SERIA_PATTERN = "seria";
    
    public const string SERIA_NUMBER_PATTERN = "seria_number";

    public static string Build(string pattern, Book book) {
        var map = new Dictionary<string, string> {
            { TITLE_PATTERN, book.Title },
            { AUTHOR_PATTERN, book.Author.Name },
            { SERIA_PATTERN, book.Seria?.Name },
            { SERIA_NUMBER_PATTERN, book.Seria?.Number },
        };
        
        foreach (var (key, value) in map) {
            pattern = pattern.Replace("{" + key + "}", value.RemoveInvalidChars());
        }

        return pattern.RemoveInvalidChars().Crop(100);
    }
}