using System;
using System.Net.Http;

namespace Elib2Ebook.Configs; 

public class BookGetterConfig : IDisposable {
    public HttpClient Client { get; }

    public Options Options { get; }
    
    public bool HasCredentials => !string.IsNullOrWhiteSpace(Options.Login) && !string.IsNullOrWhiteSpace(Options.Password);

    public BookGetterConfig(Options options, HttpClient client){
        Client = client;
        Options = options;
    }

    public void Dispose() {
        Client?.Dispose();
    }
}