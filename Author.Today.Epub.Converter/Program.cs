using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Author.Today.Epub.Converter.Logic;
using Author.Today.Epub.Converter.Types;
using CommandLine;

namespace Author.Today.Epub.Converter {
    class Program {
        private static async Task Main(string[] args) {
            await Parser.Default.ParseArguments<Options>(args)
                .WithParsedAsync(async options => {
                    var handler = new HttpClientHandler {
                        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
                    };
            
                    if (!string.IsNullOrEmpty(options.Proxy)) {
                        var split = options.Proxy.Split(":");
                        handler.Proxy = new WebProxy(split[0], int.Parse(split[1])); 
                    }

                    var client = new HttpClient(handler);
                    var pattern = await File.ReadAllTextAsync("ChapterPattern.xhtml");
                    
                    using var getter = new BookGetter(client, pattern);
                    var book = await getter.Get(options.BookId);

                    var generator = new EpubGenerator(options.SavePath);
                    generator.Generate(book);
                });
        }
    }
}
