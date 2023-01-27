using System;
using System.Net;
using System.Net.Http;

namespace Elib2Ebook.Configs; 

public class BookGetterConfig : IDisposable {
    public HttpClient Client { get; }

    public Options Options { get; }
    
    public CookieContainer CookieContainer { get; }
    
    public bool HasCredentials => !string.IsNullOrWhiteSpace(Options.Login) && !string.IsNullOrWhiteSpace(Options.Password);

    public TempFolder.TempFolder TempFolder { get; }

    public BookGetterConfig(Options options, HttpClient client, CookieContainer cookieContainer, TempFolder.TempFolder tempFolder){
        Client = client;
        CookieContainer = cookieContainer;
        Options = options;
        TempFolder = tempFolder;

        // Client.DefaultRequestHeaders.Add("sec-ch-ua","\"Not_A Brand\";v=\"99\", \"Google Chrome\";v=\"109\", \"Chromium\";v=\"109\"");
        Client.DefaultRequestHeaders.Add("Sec-Ch-ua-Mobile","?0");
        Client.DefaultRequestHeaders.Add("Sec-Ch-ua-Platform","linux");
        Client.DefaultRequestHeaders.Add("Sec-Fetch-Dest","document");
        Client.DefaultRequestHeaders.Add("Sec-Fetch-Mode","navigate");
        Client.DefaultRequestHeaders.Add("Sec-Fetch-Site","none");
        Client.DefaultRequestHeaders.Add("Sec-Fetch-User","?1");
        Client.DefaultRequestHeaders.Add("User-Agent","Mozilla/5.0 (compatible; YandexBot/3.0; +http://yandex.com/bots)");

        
        // Client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (compatible; YandexBot/3.0; +http://yandex.com/bots)");
        Client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
        Client.DefaultRequestHeaders.Add("Accept-Language", "ru");
        Client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip");
    }

    public void Dispose() {
        Client?.Dispose();
        TempFolder?.Dispose();
    }
}