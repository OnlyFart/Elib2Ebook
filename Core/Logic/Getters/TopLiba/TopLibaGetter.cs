using System;
using System.Threading.Tasks;
using Core.Configs;
using Core.Extensions;
using Core.Types.Book;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;

namespace Core.Logic.Getters.TopLiba; 

public class TopLibaGetter : TopLibaGetterBase {
    public TopLibaGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://topliba.com/");

    protected override Seria GetSeria(HtmlDocument doc, Uri url) {
        var a = doc.QuerySelector("div.book-series a");
        if (a != default) {
            var text = a.GetText();
            var number = a.NextSibling;
            
            if (number != default && number.GetText().Contains('#')) {
                return new Seria {
                    Name = text,
                    Number = number.GetText().Replace("(", "").Replace(")", "").Replace("#", ""),
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
        var imagePath = doc.QuerySelector("img[itemprop=contentUrl]")?.Attributes["src"]?.Value;
        return !string.IsNullOrWhiteSpace(imagePath) ? SaveImage(uri.MakeRelativeUri(imagePath)) : Task.FromResult(default(Image));
    }
}