using System;
using System.Linq;
using System.Threading.Tasks;
using Elib2Ebook.Configs;
using Elib2Ebook.Extensions;
using Elib2Ebook.Types.Book;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;

namespace Elib2Ebook.Logic.Getters.TopLiba; 

public class BiglibaGetter : TopLibaGetterBase {
    public BiglibaGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://bigliba.com/");

    protected override Seria GetSeria(HtmlDocument doc, Uri url) {
        var a = doc.QuerySelector("div.book-series a");
        if (a != default) {
            var text = a.GetText();
            
            if (text.Contains('#')) {
                var parts = text.Split(':').Last().Split("(#");
                return new Seria {
                    Name = parts[0].Trim(),
                    Number = parts[1].Trim(')').Trim(),
                    Url = url.MakeRelativeUri(a.Attributes["href"].Value)
                };
            }

            return new Seria {
                Name = text,
                Url = url.MakeRelativeUri(a.Attributes["href"].Value)
            };
        }

        return default;
    }

    protected override Task<Image> GetCover(HtmlDocument doc, Uri uri) {
        var imagePath = doc.QuerySelector("img[itemprop=image]")?.Attributes["src"]?.Value;
        return !string.IsNullOrWhiteSpace(imagePath) ? GetImage(uri.MakeRelativeUri(imagePath)) : Task.FromResult(default(Image));
    }
}