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

        Client.DefaultRequestHeaders.Add("sec-ch-ua","\"Not_A Brand\";v=\"99\", \"Google Chrome\";v=\"109\", \"Chromium\";v=\"109\"");
        Client.DefaultRequestHeaders.Add("sec-ch-ua-mobile","?0");
        Client.DefaultRequestHeaders.Add("sec-ch-ua-platform","linux");
        Client.DefaultRequestHeaders.Add("sec-fetch-dest","document");
        Client.DefaultRequestHeaders.Add("sec-fetch-mode","navigate");
        Client.DefaultRequestHeaders.Add("sec-fetch-site","none");
        Client.DefaultRequestHeaders.Add("sec-fetch-user","?1");
        Client.DefaultRequestHeaders.Add("user-agent","Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/109.0.0.0 Safari/537.36");
    }

    public void Dispose() {
        Client?.Dispose();
        TempFolder?.Dispose();
    }
}