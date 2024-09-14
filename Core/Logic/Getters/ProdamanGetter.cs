using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Core.Configs;
using Core.Extensions;
using Core.Types.Book;
using Core.Types.Common;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using Microsoft.Extensions.Logging;

namespace Core.Logic.Getters;

public class ProdamanGetter : GetterBase {
    public ProdamanGetter(BookGetterConfig config) : base(config) {
        InitMap();
    }

    protected override Uri SystemUrl => new("https://prodaman.ru/");
    
    private static readonly Dictionary<int, char> Map = new();

    private static void Append(string str, int start) {
        for (var i = 0; i < str.Length; i++) {
            Map[start + i] = str[i];
        }
    }
    
    /// <summary>
    /// Авторизация в системе
    /// </summary>
    /// <exception cref="Exception"></exception>
    public override async Task Authorize(){
        if (!Config.HasCredentials) {
            return;
        }

        var doc = await Config.Client.PostHtmlDocWithTriesAsync(SystemUrl.MakeRelativeUri("/login"), GetAuthData());

        if (!string.IsNullOrWhiteSpace(doc.GetTextBySelector("p.error"))) {
            throw new Exception("Не удалось авторизоваться");
        }
    }
    
    private FormUrlEncodedContent GetAuthData() {
        var data = new Dictionary<string, string> {
            ["email"] = Config.Options.Login,
            ["pass"] = Config.Options.Password,
            ["remember"] = "on",
        };

        return new FormUrlEncodedContent(data);
    }
    
    // MAAAAAAGIC!!!
    private static void InitMap() {
        Append("ЕКЦДНСИПТЬЧЖФ", 0x02CB);
        Append("ЗЭЯГЛЙОЮУХРМЩ", 0x02DE);
        Append("е", 0x02EC);
        Append("Ш", 0x02EF);
        Append("В", 0x038B);
        Append("А", 0x038D);
        Append("г", 0x0398);
        Append("в", 0x039E);
        Append("Б", 0x03A2);
        Append("дк", 0x03B9);
        Append("ажовпмдёфетг.с,", 0x1E86);
        Append("нм", 0x1E97);
        Append(":ули", 0x1E9A);
        Append("а", 0x1E9F);
        Append("йби", 0x1EED);
        Append("к", 0x2016);
        Append("н", 0x201F);
        Append("акд", 0x2023);
        Append("олицпщсум", 0x2027);
        Append("э", 0x2031);
        Append("фшрты", 0x2034);
        Append("б", 0x203B);
        Append("ч", 0x203D);
        Append("яхь-ю", 0x203F);
        Append("бйтгнзюацшофр", 0x2080);
        Append("л.жчмксдубяхвтщеп", 0x208E);
        Append("р,к", 0x20A0);
        Append("а", 0x20A6);
        Append("ябжмтювзнушэг", 0x2106);
        Append("оф№ьдихщ", 0x2114);
        Append("ыейпъ", 0x211D);
        Append("ъёк", 0x2123);
        Append(".", 0x2127);
        Append("щлрц", 0x2129);
        Append("асчврознжчйп.", 0x212F);
        Append(",го", 0x213D);
        Append(":", 0x2145);
        Append(";", 0x214F);
        Append("абвгдеёжзиклм", 0x2202);
        Append("н", 0x2210);
        Append("опрсту", 0x2213);
        Append("фхц", 0x221B);
        Append("чшщэюяыйъ;", 0x221F);
        Append("ь", 0x222A);
        Append("р", 0x222E);
        Append(",п", 0x2230);
        Append(".", 0x2234);
        Append("м", 0x2237);
        Append("ск", 0x223C);
        Append("н,о", 0x2240);
        Append("л:", 0x2244);
        Append(";", 0x224E);
        Append(",", 0x2250);
        Append("АБВГНПДИЛ", 0x25A1);
        Append("РЕЗКОСХЁЖЙМТШУЧЯЫЦФЬ:ЮЭЪЩжсюфп", 0x25AC);
        Append("л", 0x25CB);
        Append("гч", 0x25CD);
        Append("нх,ьтзщядвусцшкмеэр.бл", 0x25D0);
    }

    public override async Task<Book> Get(Uri url) {
        url = SystemUrl.MakeRelativeUri(url.AbsolutePath);
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(url);

        var title = doc.GetTextBySelector("h1");
        var book = new Book(url) {
            Cover = await GetCover(doc, url),
            Chapters = await FillChapter(url, title),
            Title = title,
            Author = GetAuthor(doc, url),
            Annotation = doc.QuerySelector("div[itemprop=description]")?.InnerHtml,
            Seria = GetSeria(doc, url)
        };

        return book;
    }

    private static Author GetAuthor(HtmlDocument doc, Uri url) {
        var a = doc.QuerySelector("a[data-widget-feisovet-author]");
        if (a == default) {
            return new Author("Prodaman");
        }

        return new Author(a.GetText(), url.MakeRelativeUri(a.Attributes["href"].Value));
    }

    private static Seria GetSeria(HtmlDocument doc, Uri url) {
        var a = doc.QuerySelector("p.blog-info a[href*=/series/]");
        if (a != default) {
            return new Seria {
                Name = a.GetText(),
                Url = url.MakeRelativeUri(a.Attributes["href"].Value)
            };
        }

        return default;
    }

    private async Task<int> GetPages(Uri url) {
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(url.AppendQueryParameter("nav", "ok"));
        var pages = doc.QuerySelectorAll("div.pageList a")
            .Where(a => int.TryParse(a.InnerText, out _))
            .Select(a => int.Parse(a.InnerText))
            .ToList();
        
        return pages.Count > 0 ? pages.Max() : 1;
    }
    
    private static bool IsHeaderStart(IEnumerable<HtmlNode> nodes) {
        var firstNode = nodes.First();
        return firstNode.Name == "h3" && !string.IsNullOrWhiteSpace(firstNode.InnerText);
    }
    
    private static Chapter CreateChapter(string title) {
        return new Chapter {
            Title = title
        };
    }
    
    private async Task AddChapter(ICollection<Chapter> chapters, Chapter chapter, StringBuilder text, Uri url) {
        if (chapter == null) {
            return;
        }
        
        var chapterDoc = text.ToString().AsHtmlDoc();
        chapter.Images = await GetImages(chapterDoc, url);
        chapter.Content = chapterDoc.DocumentNode.InnerHtml;
        chapters.Add(chapter);
    }

    private static string Decode(string encode) {
        return encode.Aggregate(new StringBuilder(), (sb, c) => sb.Append(Map.GetValueOrDefault(c, c))).ToString();
    }

    private async Task<IEnumerable<Chapter>> FillChapter(Uri url, string title) {
        var result = new List<Chapter>();
        if (Config.Options.NoChapters) {
            return result;
        }
        
        Chapter chapter = null;
        var text = new StringBuilder();

        var pages = await GetPages(url);
        var firstBr = true;
        for (var i = 1; i <= pages; i++) {
            Config.Logger.LogInformation($"Получаю страницу {i}/{pages}");

            var appendSegment = url.AppendQueryParameter("page", i);
            var page = await Config.Client.GetHtmlDocWithTriesAsync(appendSegment);
            var content = page.QuerySelector("div.blog-text").RemoveNodes("a[href*=snoska], div[id*=snoska]");
            var nodes = content.ChildNodes;
            if (i == 1 && !IsHeaderStart(nodes)) {
                chapter = CreateChapter(title);
            }

            foreach (var node in nodes) {
                if (node.Name != "h3" || (node.Name == "h3" && node.GetText() == "***")) {
                    if (node.Name == "br") {
                        if (firstBr) {
                            firstBr = false;
                            text.Append("<p>");
                        } else {
                            text.Append("</p><p>");
                        }
                        
                        continue;
                    }
                    
                    if (node.Name == "img" && node.Attributes["src"] != null) {
                        text.Append($"<img src='{node.Attributes["src"].Value}'/>");
                    } else if (!string.IsNullOrWhiteSpace(node.InnerHtml)) {
                        var pText = Decode(node.InnerHtml.HtmlDecode());
                        
                        if (node.InnerHtml.StartsWith(" ")) {
                            pText = " " + pText;
                        }

                        if (node.InnerHtml.EndsWith(" ")) {
                            pText += " ";
                        }
                        
                        text.Append(pText.CoverTag(node.Name == "#text" ? string.Empty : node.Name));
                    }
                } else {
                    text.Append("</p>");
                    await AddChapter(result, chapter, text, url);
                    text.Clear();
                    chapter = CreateChapter(Decode(node.InnerText.HtmlDecode()));
                }
            }
        }
        
        text.Append("</p>");
        await AddChapter(result, chapter ?? CreateChapter(title), text, url);
        return result;
    }

    private Task<TempFile> GetCover(HtmlDocument doc, Uri url) {
        var imagePath = doc.QuerySelector("div[itemprop=aggregateRating] img[itemprop=image]")?.Attributes["src"]?.Value;
        return !string.IsNullOrWhiteSpace(imagePath) ? SaveImage(url.MakeRelativeUri(imagePath)) : Task.FromResult(default(TempFile));
    }
}