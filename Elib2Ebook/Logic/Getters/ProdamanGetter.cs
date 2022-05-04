using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Elib2Ebook.Configs;
using Elib2Ebook.Extensions;
using Elib2Ebook.Types.Book;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;

namespace Elib2Ebook.Logic.Getters;

public class ProdamanGetter : GetterBase {
    public ProdamanGetter(BookGetterConfig config) : base(config) {
        InitMap();
    }
    protected override Uri SystemUrl => new("https://prodaman.ru/");
    
    private static readonly Dictionary<int, char> _map = new();

    private static void Append(string str, int start) {
        for (int i = 0; i < str.Length; i++) {
            _map[start + i] = str[i];
        }
    }
    
    /// <summary>
    /// Авторизация в системе
    /// </summary>
    /// <exception cref="Exception"></exception>
    private async Task Authorize(){
        if (!_config.HasCredentials) {
            return;
        }
        
        
        using var post = await _config.Client.PostWithTriesAsync(new Uri("https://prodaman.ru/login"), GetAuthData());
        var doc = await post.Content.ReadAsStringAsync().ContinueWith(t => t.Result.AsHtmlDoc());
        
        if (!string.IsNullOrWhiteSpace(doc.GetTextBySelector("p.error"))) {
            throw new Exception("Не удалось авторизоваться");
        }
    }
    
    private FormUrlEncodedContent GetAuthData() {
        var data = new Dictionary<string, string> {
            ["email"] = _config.Login,
            ["pass"] = _config.Password,
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
        Init();

        await Authorize();
        url = new Uri(SystemUrl, url.AbsolutePath);
        var doc = await _config.Client.GetHtmlDocWithTriesAsync(url);

        var title = doc.GetTextBySelector("h1");
        var book = new Book {
            Cover = await GetCover(doc, url),
            Chapters = await FillChapter(url, title),
            Title = title,
            Author = doc.GetTextBySelector("a[data-widget-feisovet-author]") ?? "Prodaman",
            Annotation = doc.GetTextBySelector("div[itemprop=description]").CollapseWhitespace()
        };

        return book;
    }

    private async Task<int> GetPages(Uri url) {
        var doc = await _config.Client.GetHtmlDocWithTriesAsync(new Uri(url.AbsoluteUri + "?nav=ok"));
        var pages = doc.QuerySelectorAll("div.pageList a")
            .Where(a => int.TryParse(a.InnerText, out _))
            .Select(a => int.Parse(a.InnerText))
            .ToList();
        return pages.Count > 0 ? pages.Max() : 1;
    }
    
    private static bool IsSingleChapter(IEnumerable<HtmlNode> nodes) {
        var firstNode = nodes.First();
        return firstNode.Name != "h3" || string.IsNullOrWhiteSpace(firstNode.InnerText);
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
        
        var chapterDoc = text.ToString().HtmlDecode().AsHtmlDoc();
        chapter.Images = await GetImages(chapterDoc, url);
        chapter.Content = chapterDoc.DocumentNode.InnerHtml;
        chapters.Add(chapter);
    }

    private string Decode(string encode) {
        var sb = new StringBuilder();
        foreach (var c in encode) {
            sb.Append(_map.TryGetValue(c, out var d) ? d : c);
        }

        return sb.ToString();
    }

    private async Task<IEnumerable<Chapter>> FillChapter(Uri url, string title) {
        var chapters = new List<Chapter>();
        Chapter chapter = null;
        var singleChapter = true;
        var text = new StringBuilder();

        var pages = await GetPages(url);
        for (var i = 1; i <= pages; i++) {
            Console.WriteLine($"Получаю страницу {i}/{pages}");
            
            var page = await _config.Client.GetHtmlDocWithTriesAsync(new Uri(url.AbsoluteUri + $"?page={i}"));
            var content = page.QuerySelector("div.blog-text");
            var nodes = content.ChildNodes;
            singleChapter = i == 1 ? IsSingleChapter(nodes) : singleChapter;

            foreach (var node in nodes) {
                if (singleChapter || node.Name != "h3") {
                    if (!string.IsNullOrWhiteSpace(node.InnerText)) {
                        text.Append($"<p>{Decode(node.InnerText.HtmlDecode().HtmlEncode())}</p>");
                    }
                } else {
                    await AddChapter(chapters, chapter, text, url);
                    text.Clear();
                    chapter = CreateChapter(Decode(node.InnerText.HtmlDecode().HtmlEncode()));
                    Console.WriteLine(chapter.Title);
                }
            }
        }

        await AddChapter(chapters, chapter ?? CreateChapter(title), text, url);
        return chapters;
    }

    private Task<Image> GetCover(HtmlDocument doc, Uri url) {
        var imagePath = doc.QuerySelector("div[itemprop=aggregateRating] img[itemprop=image]")?.Attributes["src"]?.Value;
        return !string.IsNullOrWhiteSpace(imagePath) ? GetImage(new Uri(url, imagePath)) : Task.FromResult(default(Image));
    }
}