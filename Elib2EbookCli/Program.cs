using System.Text;
using CommandLine;
using Core.Configs;
using Core.Extensions;
using Core.Misc;
using Microsoft.Extensions.Logging;

namespace Elib2EbookCli; 

internal static class Program {
    private static async Task Main(string[] args) {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        await Parser.Default.ParseArguments<Options>(args)
            .WithParsedAsync(async options => {
                var logger = new ConsoleLogger();
                
                using var getterConfig = BookGetterConfig.GetDefault(options, logger); 
                using var getter = GetterProvider.Get(getterConfig, options.Url.First().AsUri());
                await getter.Init();
                await getter.Authorize();

                foreach (var url in options.Url) {
                    logger.LogInformation($"Начинаю генерацию книги {url.CoverQuotes()}");
                    try {
                        var book = await getter.Get(url.AsUri());
                        foreach (var format in options.Format) {
                            await BuilderProvider.Get(format, options, logger).Build(book);
                        }
                    } catch (Exception ex) {
                        logger.LogInformation($"Генерация книги {url} завершилась с ошибкой. {ex}");
                    }
                }
            });
    }
}