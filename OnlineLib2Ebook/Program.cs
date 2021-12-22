using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using OnlineLib2Ebook.Configs;
using OnlineLib2Ebook.Logic.Builders;
using OnlineLib2Ebook.Logic.Getters;

namespace OnlineLib2Ebook {
    class Program {
        private static async Task Main(string[] args) {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Console.OutputEncoding = Encoding.UTF8;

            await Parser.Default.ParseArguments<Options>(args)
                .WithParsedAsync(async options => {
                    var handler = new HttpClientHandler {
                        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate |
                                                 DecompressionMethods.Brotli
                    };

                    if (!string.IsNullOrEmpty(options.Proxy)) {
                        var split = options.Proxy.Split(":");
                        handler.Proxy = new WebProxy(split[0], int.Parse(split[1]));
                    }

                    var client = new HttpClient(handler);
                    var url = new Uri(options.Url);

                    var getterConfig = new BookGetterConfig(options, client);
                    using var getter = GetGetter(getterConfig, url);

                    var book = await getter.Get(url);
                    book.Save(GetBuilder(options.Format), options.SavePath, "Patterns");
                });
        }

        private static BuilderBase GetBuilder(string format) {
            return format switch {
                "fb2" => Fb2Builder.Create(),
                "epub" => EpubBuilder.Create(File.ReadAllText("Patterns/ChapterPattern.xhtml")),
                _ => throw new ArgumentException("Неизвестный формат", nameof(format))
            };
        }

        private static GetterBase GetGetter(BookGetterConfig config, Uri url) {
            return Assembly.GetAssembly(typeof(GetterBase))!.GetTypes()
                       .Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(typeof(GetterBase)))
                       .Select(type => (GetterBase) Activator.CreateInstance(type, config))
                       .FirstOrDefault(g => g!.IsSameUrl(url)) ??
                   throw new ArgumentException("Данная система не поддерживается", nameof(url));
        }
    }
}
