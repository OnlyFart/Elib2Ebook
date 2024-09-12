using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Core.Extensions;
using Core.Misc.TempFolder;
using Microsoft.Extensions.Logging;

namespace Core.Configs; 

public class BookGetterConfig : IDisposable {
    public readonly ILogger Logger;

    private class RedirectHandler : DelegatingHandler {
        private readonly ILogger _logger;

        public RedirectHandler(HttpMessageHandler innerHandler, ILogger logger) {
            _logger = logger;
            InnerHandler = innerHandler;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
            try {
                var responseMessage = await base.SendAsync(request, cancellationToken);

                if (responseMessage is {
                        StatusCode: HttpStatusCode.Redirect or HttpStatusCode.PermanentRedirect or HttpStatusCode.MovedPermanently or HttpStatusCode.Moved, Headers.Location: not null
                    }) {
                    request = new HttpRequestMessage(request.Method, responseMessage.Headers.Location);
                    responseMessage = await base.SendAsync(request, cancellationToken);
                }

                return responseMessage;
            } catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException) {
                _logger.LogInformation("Сервер не успевает ответить. Попробуйте увеличить Timeout с помощью параметра -t");
                throw;
            } catch (Exception ex) {
                _logger.LogInformation(ex.Message);
                throw;
            }
        }
    }
    
    public HttpClient Client { get; }

    public Options Options { get; }
    
    public CookieContainer CookieContainer { get; }
    
    public bool HasCredentials => !string.IsNullOrWhiteSpace(Options.Login) && !string.IsNullOrWhiteSpace(Options.Password);

    public TempFolder TempFolder { get; }

    public BookGetterConfig(Options options, HttpClient client, CookieContainer cookieContainer, TempFolder tempFolder, ILogger logger) {
        Logger = logger;
        Client = client;
        CookieContainer = cookieContainer;
        Options = options;
        TempFolder = tempFolder;
    }

    public void Dispose() {
        Client?.Dispose();
        TempFolder?.Dispose();
    }
    
    public static BookGetterConfig GetDefault(Options options, ILogger logger) {
        var cookieContainer = new CookieContainer();
        var client = GetClient(options, cookieContainer, logger);

        return new BookGetterConfig(options, client, cookieContainer, TempFolderFactory.Create(options.TempPath, !options.SaveTemp), logger); 
    }
    
    private static HttpClient GetClient(Options options, CookieContainer container, ILogger logger) {
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

        var client = new HttpClient(new RedirectHandler(handler, logger));
        client.Timeout = TimeSpan.FromSeconds(options.Timeout);
        return client;
    }
}