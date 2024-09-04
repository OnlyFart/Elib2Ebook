using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Core.Configs;
using Core.Extensions;
using Core.Types.Book;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using Microsoft.Extensions.Logging;

namespace Core.Logic.Getters.Novelxo; 

public abstract class NovelxoGetterBase : GetterBase {
    private static readonly byte[] Key = StringToByteArray("61626326312a7e235e325e2373305e3d295e5e3725623334");
    private static readonly byte[] IV = StringToByteArray("31323334353637383930383533373237");
    
    protected NovelxoGetterBase(BookGetterConfig config) : base(config) { }

    protected override string GetId(Uri url) => url.GetSegment(1);

    public override async Task<Book> Get(Uri url) {
        url = await GetMainUrl(url);
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(url);
        
        var book = new Book(url) {
            Cover = await GetCover(doc, url),
            Chapters = await FillChapters(doc, url),
            Title = doc.GetTextBySelector("h1"),
            Author = GetAuthor(doc, url),
            Annotation = GetAnnotation(doc)
        };
            
        return book;
    }

    private async Task<Uri> GetMainUrl(Uri url) {
        if (url.Segments.Length == 2) {
            return SystemUrl.MakeRelativeUri(GetId(url));
        }

        var doc = await Config.Client.GetHtmlDocWithTriesAsync(url);
        return url.MakeRelativeUri(doc.QuerySelector("div.readerheader-wg a").Attributes["href"].Value);
    }

    private async Task<IEnumerable<Chapter>> FillChapters(HtmlDocument doc, Uri url) {
        var result = new List<Chapter>();
        if (Config.Options.NoChapters) {
            return result;
        }
        
        var start = url.MakeRelativeUri(doc.QuerySelector("div.actions a.read").Attributes["href"].Value);
        
        while (true) {
            var sb = new StringBuilder();
            doc = await Config.Client.GetHtmlDocWithTriesAsync(start);

            var container = doc.QuerySelector("div.readerbody-wg");
            foreach (var node in container.QuerySelectorAll("p, div.ctp")) {
                if (node.Name == "p") {
                    sb.Append(node.InnerHtml.HtmlDecode().CoverTag(node.Name));
                } else if (node.Name == "div" && node.Attributes["class"]?.Value?.Contains("ctp") == true) {
                    if (node.Attributes["class"].Value.Contains("protected")) {
                        sb.Append(await GetProtected(doc, node));
                    } else {
                        sb.Append(Encoding.UTF8.GetString(Transform(Convert.FromBase64String(node.InnerText), Key, IV)).HtmlDecode());
                    }
                }
            }

            var chapterDoc = sb.AsHtmlDoc();
            var chapter = new Chapter();
            chapter.Title = doc.GetTextBySelector("h2.chapter-title");
            chapter.Images = await GetImages(chapterDoc, start);
            chapter.Content = chapterDoc.DocumentNode.InnerHtml;

            result.Add(chapter);
            
            Config.Logger.LogInformation($"Загружена глава {chapter.Title.CoverQuotes()}");

            var next = doc.QuerySelector("div.readernav-wg a.next");
            if (next == default) {
                return result;
            }

            start = start.MakeRelativeUri(next.Attributes["href"].Value);
        }
    }
    
    private async Task<string> GetProtected(HtmlDocument doc, HtmlNode node) {
        var id = node.Attributes["data-id"].Value;
        var header = Regex.Match(doc.ParsedText, @"http.setRequestHeader\((?<name>.*?), '(?<value>.*?)'");
        
        var headerName = Regex.Match(doc.ParsedText, $"{header.Groups["name"].Value} = \'(?<id>.*?)\'").Groups["id"].Value;
        var headerValue = header.Groups["value"].Value;
        var key = StringToByteArray(Regex.Match(doc.ParsedText, "signKey = \'(?<id>.*?)\'").Groups["id"].Value);

        var encode = Transform(Encoding.UTF8.GetBytes(id), key, IV);

        var url = new Uri($"https://a.novelxo.com/v1/chapters/{id}/ctp?width=1140&sign=932870{Atob(Convert.ToBase64String(encode))}182906");
        var message = new HttpRequestMessage(HttpMethod.Get, url);
        message.Headers.Add(headerName, headerValue);
        message.Headers.Add("referer", SystemUrl.ToString());

        var response = await Config.Client.SendAsync(message);
        var readAsStringAsync = await response.Content.ReadAsStringAsync();
        var result = Encoding.UTF8.GetString(Transform(Convert.FromBase64String(readAsStringAsync.Trim('\"')), Key, IV)).HtmlDecode();

        return result;
    }

    private static string Atob(string toEncode) {
        return Convert.ToBase64String(Encoding.GetEncoding(28591).GetBytes(toEncode));
    }

    private static string GetAnnotation(HtmlDocument doc) {
        var html = doc.QuerySelector("#bookinfo")?.InnerHtml;
        return string.IsNullOrWhiteSpace(html) ? 
            string.Empty : 
            html.AsHtmlDoc().RemoveNodes("script, ins").DocumentNode.InnerHtml;
    }
    
    private Task<Image> GetCover(HtmlDocument doc, Uri uri) {
        var imagePath = doc.QuerySelector("div.cover img")?.Attributes["data-src"]?.Value;
        return !string.IsNullOrWhiteSpace(imagePath) ? SaveImage(uri.MakeRelativeUri(imagePath)) : Task.FromResult(default(Image));
    }
    
    private static Author GetAuthor(HtmlDocument doc, Uri url) {
        var a = doc.QuerySelector("tr.authors a");
        return a == default ? 
            new Author("Novelxo") : 
            new Author(a.GetText(), url.MakeRelativeUri(a.Attributes["href"].Value));
    }

    private static byte[] StringToByteArray(string hex) {
        return Enumerable.Range(0, hex.Length)
            .Where(x => x % 2 == 0)
            .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
            .ToArray();
    }

    private static void AesCtrTransform(byte[] key, byte[] salt, Stream inputStream, Stream outputStream) {
        var aes = Aes.Create();
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.None;

        var blockSize = aes.BlockSize / 8;
        var counter = (byte[])salt.Clone();
        var xorMask = new Queue<byte>();

        var zeroIv = new byte[blockSize];
        var counterEncryptor = aes.CreateEncryptor(key, zeroIv);

        int b;
        while ((b = inputStream.ReadByte()) != -1) {
            if (xorMask.Count == 0) {
                var counterModeBlock = new byte[blockSize];

                counterEncryptor.TransformBlock(counter, 0, counter.Length, counterModeBlock, 0);

                for (var i = counter.Length - 1; i >= 0; i--) {
                    if (++counter[i] != 0) {
                        break;
                    }
                }

                foreach (var b2 in counterModeBlock) {
                    xorMask.Enqueue(b2);
                }
            }

            var mask = xorMask.Dequeue();
            outputStream.WriteByte((byte)((byte)b ^ mask));
        }
    }

    private static byte[] Transform(byte[] str, byte[] key, byte[] iv) {
        using var outputEncryptedStream = new MemoryStream(str);
        using var outputDecryptedStream = new MemoryStream();
        AesCtrTransform(key, iv, outputEncryptedStream, outputDecryptedStream);

        outputDecryptedStream.Position = 0;
        return outputDecryptedStream.ToArray();
    }
}