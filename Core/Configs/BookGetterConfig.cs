using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Core.Extensions;
using Core.Misc.TempFolder;

namespace Core.Configs; 

public class BookGetterConfig : IDisposable {
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
    
    public HttpClient Client { get; }

    public Options Options { get; }
    
    public CookieContainer CookieContainer { get; }
    
    public bool HasCredentials => !string.IsNullOrWhiteSpace(Options.Login) && !string.IsNullOrWhiteSpace(Options.Password);

    public TempFolder TempFolder { get; }

    public BookGetterConfig(Options options, HttpClient client, CookieContainer cookieContainer, TempFolder tempFolder) {
        Client = client;
        CookieContainer = cookieContainer;
        Options = options;
        TempFolder = tempFolder;
    }

    public void Dispose() {
        Client?.Dispose();
        TempFolder?.Dispose();
    }
    
    public static BookGetterConfig GetDefault(Options options) {
        var cookieContainer = new CookieContainer();
        var client = GetClient(options, cookieContainer);

        return new BookGetterConfig(options, client, cookieContainer, TempFolderFactory.Create(options.TempPath, !options.SaveTemp)); 
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
}