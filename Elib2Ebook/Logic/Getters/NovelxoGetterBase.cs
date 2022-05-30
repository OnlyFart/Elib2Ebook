using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Elib2Ebook.Configs;
using Elib2Ebook.Extensions;
using Elib2Ebook.Types.Book;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;

namespace Elib2Ebook.Logic.Getters; 

public abstract class NovelxoGetterBase : GetterBase {
    private static readonly byte[] Key = StringToByteArray("61626326312a7e235e325e2373305e3d295e5e3725623334");
    private static readonly byte[] IV = StringToByteArray("31323334353637383930383533373237");
    
    protected NovelxoGetterBase(BookGetterConfig config) : base(config) { }

    protected override string GetId(Uri url) {
        return url.Segments[1].Trim('/');
    }

    public override async Task<Book> Get(Uri url) {
        url = await GetMainUrl(url);
        var doc = await _config.Client.GetHtmlDocWithTriesAsync(url);
        
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
            return new Uri($"https://{SystemUrl.Host}/{GetId(url)}");;
        }

        var doc = await _config.Client.GetHtmlDocWithTriesAsync(url);
        return new Uri(url, doc.QuerySelector("div.readerheader-wg a").Attributes["href"].Value);
    }

    private async Task<IEnumerable<Chapter>> FillChapters(HtmlDocument doc, Uri url) {
        var result = new List<Chapter>();
        var start = new Uri(url, doc.QuerySelector("div.actions a.read").Attributes["href"].Value);
        
        while (true) {
            var sb = new StringBuilder();
            doc = await _config.Client.GetHtmlDocWithTriesAsync(start);

            foreach (var node in doc.QuerySelector("div.readerbody-wg").ChildNodes) {
                if (node.Name == "p") {
                    sb.Append($"<p>{node.InnerHtml.HtmlDecode()}</p>");
                } else if (node.Name == "div" && node.Attributes["class"]?.Value?.Contains("ctp") == true) {
                    sb.Append(Decode(node.InnerText).HtmlDecode());
                }
            }

            var chapterDoc = sb.AsHtmlDoc();
            var chapter = new Chapter();
            chapter.Title = doc.GetTextBySelector("h2.chapter-title");
            chapter.Images = await GetImages(chapterDoc, start);
            chapter.Content = chapterDoc.DocumentNode.InnerHtml;

            result.Add(chapter);
            
            Console.WriteLine($"Загружена глава {chapter.Title.CoverQuotes()}");

            var next = doc.QuerySelector("div.readernav-wg a.next");
            if (next == default) {
                return result;
            }

            start = new Uri(start, next.Attributes["href"].Value);
        }
    }

    private static string GetAnnotation(HtmlDocument doc) {
        var html = doc.QuerySelector("#bookinfo")?.InnerHtml;
        return string.IsNullOrWhiteSpace(html) ? 
            string.Empty : 
            html.AsHtmlDoc().RemoveNodes("script, ins").DocumentNode.InnerHtml;
    }
    
    private Task<Image> GetCover(HtmlDocument doc, Uri uri) {
        var imagePath = doc.QuerySelector("div.cover img")?.Attributes["data-src"]?.Value;
        return !string.IsNullOrWhiteSpace(imagePath) ? GetImage(new Uri(uri, imagePath)) : Task.FromResult(default(Image));
    }
    
    private static Author GetAuthor(HtmlDocument doc, Uri url) {
        var a = doc.QuerySelector("tr.authors a");
        return a == default ? 
            new Author("Novelxo") : 
            new Author(a.GetTextBySelector(), new Uri(url, a.Attributes["href"].Value));
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

    private static string Decode(string encode) {
        using var outputEncryptedStream = new MemoryStream(Convert.FromBase64String(encode));
        using var outputDecryptedStream = new MemoryStream();
        AesCtrTransform(Key, IV, outputEncryptedStream, outputDecryptedStream);

        outputDecryptedStream.Position = 0;
        using var reader = new StreamReader(outputDecryptedStream);
        return reader.ReadToEnd().HtmlDecode();
    }
}