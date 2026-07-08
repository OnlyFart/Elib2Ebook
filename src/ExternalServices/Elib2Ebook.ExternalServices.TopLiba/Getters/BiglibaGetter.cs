using Elib2Ebook.Domain.Book;
using Elib2Ebook.Domain.Common;
using Elib2Ebook.DomainServices.Configs;
using Elib2Ebook.DomainServices.Extensions;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;

namespace Elib2Ebook.ExternalServices.TopLiba.Getters;

public class BiglibaGetter(BookGetterConfig config) : TopLibaGetterBase(config)
{
    protected override Uri SystemUrl => new("https://bigliba.com/");

    protected override Seria GetSeria(HtmlDocument doc, Uri url)
    {
        var a = doc.QuerySelector("div.book-series a");
        if (a != null)
        {
            var text = a.GetText();

            if (text.Contains('#'))
            {
                var parts = text.Split(':').Last().Split("(#");
                return new Seria
                {
                    Name = parts[0].Trim(), Number = parts[1].Trim(')').Trim(), Url = url.MakeRelativeUri(a.Attributes["href"].Value)
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
        var imagePath = doc.QuerySelector("img[itemprop=image]")?.Attributes["src"]?.Value;
        return !string.IsNullOrWhiteSpace(imagePath) ? SaveImage(uri.MakeRelativeUri(imagePath)) : Task.FromResult(default(TempFile));
    }
}
