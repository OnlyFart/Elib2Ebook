using System.Net;
using System.Reflection;
using System.Text;
using CommandLine;
using Core.Configs;
using Core.Extensions;
using Core.Logic.Builders;
using Core.Logic.Getters;
using Core.Misc.TempFolder;

namespace Elib2EbookCli; 

internal static class Program {
    private class RedirectHandler : DelegatingHandler {
        public RedirectHandler(HttpMessageHandler innerHandler) => InnerHandler = innerHandler;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
            var responseMessage = await base.SendAsync(request, cancellationToken);
        
            if (responseMessage is { StatusCode: HttpStatusCode.Redirect or HttpStatusCode.PermanentRedirect or HttpStatusCode.MovedPermanently or HttpStatusCode.Moved, Headers.Location: not null }) {
                request = new HttpRequestMessage(HttpMethod.Get, responseMessage.Headers.Location);
                responseMessage = await base.SendAsync(request, cancellationToken);
            }

            return responseMessage;
        }
    }
    
    private static async Task Main(string[] args) {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Console.OutputEncoding = Encoding.UTF8;

        await Parser.Default.ParseArguments<Options>(args)
            .WithParsedAsync(async options => {
                var cookieContainer = new CookieContainer();
                using var client = GetClient(options, cookieContainer);

                using var getterConfig = new BookGetterConfig(options, client, cookieContainer, TempFolderFactory.Create(options.TempPath, !options.SaveTemp)); 
                using var getter = GetGetter(getterConfig, options.Url.First().AsUri());
                await getter.Init();
                await getter.Authorize();

                foreach (var url in options.Url) {
                    Console.WriteLine($"Начинаю генерацию книги {url.CoverQuotes()}");
                    try {
                        var book = await getter.Get(url.AsUri());
                        foreach (var format in options.Format) {
                            await GetBuilder(format, options).Build(book);
                        }
                    } catch (Exception ex) {
                        Console.WriteLine($"Генерация книги {url} завершилась с ошибкой. {ex}");
                    }
                }
            });
    }

    private static HttpClient GetClient(Options options, CookieContainer container) {
        var handler = new HttpClientHandler {
            AutomaticDecompression = DecompressionMethods.GZip | 
                                     DecompressionMethods.Deflate |
                                     DecompressionMethods.Brotli,
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true,
            CookieContainer = container,
            Proxy = null,
            UseProxy = false,
        };

        if (!string.IsNullOrEmpty(options.Proxy)) {
            handler.Proxy = new WebProxy(options.Proxy.AsUri());
            handler.UseProxy = true;
        }

        var client = new HttpClient(new RedirectHandler(handler));
        client.Timeout = TimeSpan.FromSeconds(options.Timeout);
        return client;
    }

    private static BuilderBase GetBuilder(string format, Options options) {
        return format.Trim().ToLower() switch {
            "fb2" => new Fb2Builder(options),
            "epub" => new EpubBuilder(options),
            "json" => new JsonBuilder(options),
            "cbz" => new CbzBuilder(options),
            "txt" => new TxtBuilder(options),
            "json_lite" => new JsonLiteBuilder(options),
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