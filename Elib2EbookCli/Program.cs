using System.Text;
using CommandLine;
using Core.Configs;
using Core.Extensions;
using Core.Misc;

namespace Elib2EbookCli; 

internal static class Program {
    private static async Task Main(string[] args) {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Console.OutputEncoding = Encoding.UTF8;

        await Parser.Default.ParseArguments<Options>(args)
            .WithParsedAsync(async options => {
                using var getterConfig = BookGetterConfig.GetDefault(options); 
                using var getter = GetterProvider.Get(getterConfig, options.Url.First().AsUri());
                await getter.Init();
                await getter.Authorize();

                foreach (var url in options.Url) {
                    Console.WriteLine($"Начинаю генерацию книги {url.CoverQuotes()}");
                    try {
                        var book = await getter.Get(url.AsUri());
                        foreach (var format in options.Format) {
                            await BuilderProvider.Get(format, options).Build(book);
                        }
                    } catch (Exception ex) {
                        Console.WriteLine($"Генерация книги {url} завершилась с ошибкой. {ex}");
                    }
                }
            });
    }
}