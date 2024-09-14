using System;
using System.Linq;
using System.Threading.Tasks;
using Core.Configs;
using Core.Extensions;
using Core.Types.Book;
using Core.Types.Common;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;

namespace Core.Logic.Getters.TopLiba; 

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

    protected override Task<TempFile> GetCover(HtmlDocument doc, Uri uri) {
        var imagePath = doc.QuerySelector("img[itemprop=image]")?.Attributes["src"]?.Value;
        return !string.IsNullOrWhiteSpace(imagePath) ? SaveImage(uri.MakeRelativeUri(imagePath)) : Task.FromResult(default(TempFile));
    }
}