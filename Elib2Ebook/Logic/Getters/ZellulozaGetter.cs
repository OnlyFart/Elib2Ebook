using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Elib2Ebook.Configs;
using Elib2Ebook.Extensions;
using Elib2Ebook.Types.AuthorToday;
using Elib2Ebook.Types.Book;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;

namespace Elib2Ebook.Logic.Getters
{
    public class ZellulozaGetter : GetterBase
    {
        private string _nonce;
        private string _ze_hash = "dummy";

        public ZellulozaGetter(BookGetterConfig config) :  base(config) { }

        protected override Uri SystemUrl => new("https://zelluloza.ru");

        public override async Task Init()
        {
            await base.Init();
            var response = await Config.Client.GetWithTriesAsync(SystemUrl.MakeRelativeUri("/my/"));
            var doc = await response.Content.ReadAsStringAsync().ContinueWith(t => t.Result.AsHtmlDoc());
            
            _nonce = doc.QuerySelector("form[name=logfrm] input[name=nonce]")?.Attributes["value"]?.Value;
            if (string.IsNullOrWhiteSpace(_nonce))
            {
                throw new ArgumentException("Не удалось получить nonce", nameof(_nonce));
            }
        }

        private HttpRequestMessage GetDefaultMessage(Uri uri, Uri host, HttpContent content = null)
        {
            var message = new HttpRequestMessage(content == default ? HttpMethod.Get : HttpMethod.Post, uri);
            message.Content = content;
            return message;
        }

        public override async Task Authorize()
        {
            if (!Config.HasCredentials)
            {
                return;
            }
            var data = new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string, string>("nonce", _nonce),
                new KeyValuePair<string, string>("log2in", Config.Options.Login),
                new KeyValuePair<string, string>("q", "login"),
                new KeyValuePair<string, string>("token", ""),
                new KeyValuePair<string, string>("pas2sword", Config.Options.Password),
                new KeyValuePair<string, string>("btnvalue", "Войти"),

            };
            var content = new FormUrlEncodedContent(data);
            var response = await Config.Client.SendAsync(GetDefaultMessage(SystemUrl.AppendSegment("/"), SystemUrl, content));
            _ze_hash = Config.CookieContainer.GetAllCookies()["ze_hash"].Value;
            if(string.IsNullOrEmpty(_ze_hash) || _ze_hash == "dummy")
            {
                throw new Exception("Не удалось авторизоваться.");
            }

        }

        public override async Task<Book> Get(Uri url)
        {
            var doc = await Config.Client.GetHtmlDocWithTriesAsync(url);
#if DEBUG
            doc.Save(new FileStream("z.html", FileMode.OpenOrCreate));
#endif
            var book = new Book(url)
            {
                Title = doc.GetTextBySelector("span[itemprop=name]"),
                Cover = await GetCover(doc, url),
                Chapters = await FillChapters(url, doc),
                Author = GetAuthor(doc, url),
                Annotation = doc.QuerySelector("meta[itemprop=description]")?.Attributes["content"]?.Value,
                Seria = GetSeria(doc, url)
            };
            return book;
        }

        private Seria GetSeria(HtmlDocument doc, Uri url)
        {
            var seria = new Seria()
            {
                Name = doc.QuerySelector("p[class=jb > a[class=lnk] > b")?.InnerHtml.Replace("'", ""),
                Number = ""
            };
            return seria;
        }

        private Author GetAuthor(HtmlDocument doc, Uri url)
        {
            return new Author(
                doc.QuerySelector("span[itemprop=author] > meta[itemprop=name]")?.Attributes["content"]?.Value,
                new Uri(doc.QuerySelector("span[itemprop=author] > link[itemprop=url]")?.Attributes["href"]?.Value)
            );
        }
        private Task<Image> GetCover(HtmlDocument doc, Uri url)
        {
            var imagePath = doc.QuerySelector("meta[property=og:image]")?.Attributes["content"]?.Value;
            return !string.IsNullOrWhiteSpace(imagePath) ? SaveImage(url.MakeRelativeUri(imagePath)) : Task.FromResult(default(Image));
        }

        private async Task<IEnumerable<Chapter>> FillChapters(Uri url, HtmlDocument doc)
        {
            var result = new List<Chapter>();
            foreach (var anchor in doc.QuerySelectorAll("a[class=chptitle]"))
            {
                Console.WriteLine($"Загружаю главу {anchor.InnerHtml.CoverQuotes()}");
                var content = await GetChapContent(new Uri(url, anchor.Attributes["href"].Value));
                result.Add(new Chapter
                {
                    Title = anchor.InnerHtml,
                    Content = content,
                    Images = await GetImages(content.AsHtmlDoc(), url)
                });
            }
            return result;
        }

        private async Task<string> GetChapContent(Uri uri)
        {
            Console.WriteLine(uri);
            var id = uri.Segments[3].Trim('/');

            var doc = await Config.Client.GetHtmlDocWithTriesAsync(uri);

#if DEBUG
            doc.Save(new FileStream($"{id}.html", FileMode.OpenOrCreate));
#endif

            var page = doc.AsString();

            var re = Regex.Match(page, @"InitRead\((.*)\);\s*$", RegexOptions.Multiline);
            var vars = re.Groups[1].Value.Split(',').Select(str => str.Trim().Replace("'", "")).ToArray();
            var picsOnly = (vars[2] == "2" && vars[3] == "2");
            var numPages = int.Parse(vars[4]);
            re = Regex.Match(page, @"ajax\(\'booktext\',\s*\'\',\s*\'getbook\',([^\)]*).*\)", RegexOptions.Multiline);
            if (!re.Success)
            {
                return "<p>Глава недоступна</p>";
            }
            if (!picsOnly)
            {
                vars = re.Groups?[1].Value.Split(",").Select(str => str.Trim().Replace("'", "")).ToArray();
                return await GetChapText(uri, page, numPages, vars);
            }
            return await GetChapPics(uri, page, numPages);
        }

        private Task<string> GetChapPics(Uri uri, string page, int numPages)
        {
            throw new NotImplementedException();
        }

        private async Task<string> GetChapText(Uri uri, string page, int numPages, string[] vars)
        {
            var data = new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string, string>("op", "getbook"),
                new KeyValuePair<string, string>("par1", vars[0]),
                new KeyValuePair<string, string>("par2", vars[1]),
                new KeyValuePair<string, string>("par4",vars[2]),
            };
            var content = new FormUrlEncodedContent(data);
            var response = await Config.Client.SendAsync(GetDefaultMessage(SystemUrl.AppendSegment("/aiaxcall/"), SystemUrl, content));
            var body = await response.Content.ReadAsStringAsync();
            var encrypted = body.Split("<END>")[0].Split("\n");
            var decrypted = encrypted.Select(str => DecryptString(str)).ToArray();
            return string.Join("\n", decrypted);
        }

        private string DecryptString(string str)
        {
            var b = new Dictionary<string, string>();
            b["~"] = "0";
            b["H"] = "1";
            b["^"] = "2";
            b["@"] = "3";
            b["f"] = "4";
            b["0"] = "5";
            b["5"] = "6";
            b["n"] = "7";
            b["r"] = "8";
            b["="] = "9";
            b["W"] = "a";
            b["L"] = "b";
            b["7"] = "c";
            b[" "] = "d";
            b["u"] = "e";
            b["c"] = "f";
            var f = new List<string>();
            for (var a = 0; a < str.Length; a += 2)
            {
                f.Add(b[str.Substring(a, 1)] + b[str.Substring(a + 1, 1)]);
            };
            var ret = Hex2utf8(f);
            ret = ret.Replace("\r", "");
            if (!ret.StartsWith("[ctr]") && !ret.StartsWith("Оставьте отзыв в ленте отзывов"))
            {
                ret = "<p>" + ret.Replace("\r", "") + "</p>\n";
            }
            ret = Regex.Replace(ret, @"\[~\]([^\[]*)\[\/]", "<i>$1</i>");
            ret = Regex.Replace(ret, @"\[\*\]([^\]]*)\[\/]", "<b>$1</b>");
            ret = Regex.Replace(ret, @"\[blu\]([^\]]*)\[\/]", "<subtitle>$1</subtitle>");
            ret = Regex.Replace(ret, @"\[\*\]([^\]]*)\[\/]", "<b>$1</b>");
            ret = Regex.Replace(ret, @"\[~\]([^\[]*)\[\/]", "<i>$1</i>");
            ret = Regex.Replace(ret, @"\[blu\]([^\]]*)\[\/]", "<subtitle>$1</subtitle>");
            if (!ret.StartsWith("[ctr]") && !ret.StartsWith("Оставьте отзыв в ленте отзывов"))
            {
                ret = "<p>" + ret.Replace("\r", "") + "</p>\n";
            }
            else
            {
                ret = "";
            }
            return ret;
        }

        private string Hex2utf8(List<string> d)
        {
            var b = 0;
            var a = "";
            while (b < d.Count)
            {
                var c = Convert.ToInt16("0x0"+d[b], 16) & 255;
                if (c < 128)
                {
                    if (c < 16)
                    {
                        switch (c)
                        {
                            case 9:
                                a += " ";
                                break;
                            case 13:
                                a += "\r";
                                break;
                            case 10:
                                a += "\n";
                                break;
                        }
                    }
                    else
                    {
                        a += Char.ConvertFromUtf32(c);
                    };
                    b++;
                }
                else
                {
                    int c2;
                    int c3;
                    if ((c > 191) && (c < 224))
                    {
                        if (b + 1 < d.Count)
                        {
                            c2 = Convert.ToInt16("0x0" + d[b + 1], 16) & 255;
                            a += Char.ConvertFromUtf32(((c & 31) << 6) | (c2 & 63));
                        };
                        b += 2;
                    }
                    else
                    {
                        if (b + 2 < d.Count)
                        {
                            c2 = Convert.ToInt16("0x0" + d[b + 1], 16) & 255;
                            c3 = Convert.ToInt16("0x0" + d[b + 2], 16) & 255;
                            a += Char.ConvertFromUtf32(((c & 15) << 12) | ((c2 & 63) << 6) | (c3 & 63));
                        };
                        b += 3;
                    }
                }
            }
            return a;
        }
    }
}
