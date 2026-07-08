using Elib2Ebook.Domain.Book;
using Elib2Ebook.DomainServices.Extensions;
using StringTokenFormatter;

namespace Elib2Ebook.DomainServices;

public class BookNameBuilder
{
    public static string Build(string pattern, Book book)
    {
        var resolver = new InterpolatedStringResolver(StringTokenFormatterSettings.Default);
        var combinedContainer = resolver
            .Builder()
            .AddPrefixedObject("Book", book)
            .AddPrefixedObject("Author", book.Author)
            .AddPrefixedSingle("Seria", "HasSeria", book.Seria is not null);

        if (book.Seria is not null)
        {
            combinedContainer.AddPrefixedObject("Seria", book.Seria);
        }

        return resolver.FromContainer(pattern, combinedContainer.CombinedResult()).RemoveInvalidChars();
    }
}
