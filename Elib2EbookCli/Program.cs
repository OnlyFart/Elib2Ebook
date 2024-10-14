using System.Text;
using CommandLine;
using CommandLine.Text;
using Core.Configs;
using Core.Extensions;
using Core.Logic.Builders;
using Core.Misc;
using Microsoft.Extensions.Logging;

namespace Elib2EbookCli; 

internal static class Program {
    private static async Task Main(string[] args) {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Console.OutputEncoding = Encoding.UTF8;
        
        var logger = new ConsoleLogger();
        
        await new Parser(with => with.CaseInsensitiveEnumValues = true).ParseArguments<Options>(args)
            .WithNotParsed(errors => {
                var sentenceBuilder = SentenceBuilder.Create();
                foreach (var error in errors) {
                    logger.LogInformation(sentenceBuilder.FormatError(error));
                }
            })
            .WithParsedAsync(async options => {
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

                        if (options.Additional) {
                            await new AdditionaFileBuilder(options, logger).Build(book);
                        }

                        if (!options.SaveTemp) {
                            book.Dispose();
                        }
                    } catch (TaskCanceledException) {
                        logger.LogInformation("Сервер не успевает ответить. Попробуйте увеличить Timeout с помощью параметра -t");
                    } catch (Exception ex) {
                        logger.LogInformation($"Генерация книги {url} завершилась с ошибкой. {ex}");
                    }
                }
            });
    }
}