using Elib2Ebook.Domain.Book;
using Elib2Ebook.Domain.Common;
using Elib2Ebook.DomainServices.Configs;
using Elib2Ebook.DomainServices.Extensions;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;

namespace Elib2Ebook.ExternalServices.TopLiba.Getters;

public class TopLibaGetter(BookGetterConfig config) : TopLibaGetterBase(config)
{
    protected override Uri SystemUrl => new("https://topliba.com/");

    protected override Seria GetSeria(HtmlDocument doc, Uri url)
    {
        var a = doc.QuerySelector("div.book-series a");
        if (a != null)
        {
            var text = a.GetText();
            var number = a.NextSibling;

            if (number != null && number.GetText().Contains('#'))
            {
                return new Seria
                {
                    Name = text, Number = number.GetText().Replace("(", "").Replace(")", "").Replace("#", ""), Url = url.MakeRelativeUri(a.Attributes["href"].Value)
                };
            }

            return new Seria
            {
                Name = text, Url = url.MakeRelativeUri(a.Attributes["href"].Value)
            };
        }

        return null;
    }

    protected override Task<TempFile> GetCover(HtmlDocument doc, Uri uri)
    {
        var imagePath = doc.QuerySelector("img[itemprop=contentUrl]")?.Attributes["src"]?.Value;
        return !string.IsNullOrWhiteSpace(imagePath) ? SaveImage(uri.MakeRelativeUri(imagePath)) : Task.FromResult(default(TempFile));
    }
}
