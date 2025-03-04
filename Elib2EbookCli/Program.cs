using System.Diagnostics;
using System.Reflection;
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

        var parserResult = new Parser(c => {
            c.CaseInsensitiveEnumValues = true;
        }).ParseArguments<Options>(args);
        
        
        await parserResult
            .WithNotParsed(errs => {
                var title = Assembly.GetEntryAssembly().GetName();
                var version = FileVersionInfo.GetVersionInfo("Core.dll").ProductVersion.Split("+")[0];
                
                var heading = new HeadingInfo(title.Name, version);

                if (errs.IsVersion()) {
                    logger.LogInformation(heading);
                } else {
                    var helpText = HelpText.AutoBuild(parserResult, h => {
                        h.Heading = heading;
                        h.Copyright = string.Empty;
                        return h;
                    });
                    
                    logger.LogInformation(helpText);
                }
            })
            .WithParsedAsync(async options => {
                using var getterConfig = BookGetterConfig.GetDefault(options, logger); 
                using var getter = GetterProvider.Get(getterConfig, options.Url.First().AsUri());
                await getter.Init();
                await getter.Authorize();

                logger.LogInformation( GetterProvider.IsLibSocial( getter ) ? "lib.me" : "default" );
                logger.LogInformation( (options.SplitVolumes || options.SplitChapters) ? "split" : "monolit" );

                if( GetterProvider.IsLibSocial( getter ) && options.SplitVolumes )
                {
                    foreach (var url in options.Url) {
                        try {
                            var originalBookNamePattern = options.BookNamePattern;

                            var volumized = await getter.GetTocVolumized(url.AsUri());

                            foreach (var volume in volumized)
                            {
                                var volume_number = (volume as dynamic).Number as string;

                                options.Start = (volume as dynamic).Start;
                                options.End = (volume as dynamic).End;
                                options.BookNamePattern = string.Concat(["Volume ", volume_number, ". ", originalBookNamePattern]);

                                logger.LogInformation($"Загружаю том {volume_number}");

                                var split_book = await getter.Get(url.AsUri());
                                foreach (var format in options.Format) {
                                    await BuilderProvider.Get(format, options, logger).Build(split_book);
                                }
                                if (!options.SaveTemp) {
                                    split_book.Dispose();
                                }
                            }

                            // var book = await getter.Get(url.AsUri());
                            // if (options.Additional) {
                            //     await new AdditionaFileBuilder(options, logger).Build(book);
                            // }

                        } catch (TaskCanceledException) {
                            logger.LogInformation("Сервер не успевает ответить. Попробуйте увеличить Timeout с помощью параметра -t");
                        } catch (Exception ex) {
                            logger.LogInformation($"Генерация книги {url} завершилась с ошибкой. {ex}");
                        }
                    }
                }
                else if( GetterProvider.IsLibSocial( getter ) && options.SplitChapters )
                {
                    foreach (var url in options.Url) {
                        try {
                            var originalBookNamePattern = options.BookNamePattern;
                            var originalSavePath = options.SavePath;

                            var chaptered = await getter.GetTocChaptered(url.AsUri());

                            foreach (var chapter in chaptered)
                            {
                                var chapter_index = (chapter as dynamic).Index;
                                var chapter_number = (chapter as dynamic).Number as string;
                                var volume_number = (chapter as dynamic).Volume as string;

                                options.Start = chapter_index;
                                options.End = chapter_index;
                                options.SavePath = originalSavePath + $"\\Volume {volume_number}";
                                options.BookNamePattern = $"Chapter {chapter_number}";

                                logger.LogInformation($"Загружаю главу {chapter_number}");

                                var split_book = await getter.Get(url.AsUri());
                                foreach (var format in options.Format) {
                                    await BuilderProvider.Get(format, options, logger).Build(split_book);
                                }
                                if (!options.SaveTemp) {
                                    split_book.Dispose();
                                }
                            }

                            // var book = await getter.Get(url.AsUri());
                            // if (options.Additional) {
                            //     await new AdditionaFileBuilder(options, logger).Build(book);
                            // }

                        } catch (TaskCanceledException) {
                            logger.LogInformation("Сервер не успевает ответить. Попробуйте увеличить Timeout с помощью параметра -t");
                        } catch (Exception ex) {
                            logger.LogInformation($"Генерация книги {url} завершилась с ошибкой. {ex}");
                        }
                    }
                }
                else
                {
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
                }

            });
    }
}