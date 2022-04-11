using System;
using System.Net.Http;

namespace Elib2Ebook.Configs; 

public class BookGetterConfig : IDisposable {
    public HttpClient Client { get; }
    public string Login { get; }
    public string Password { get; }
    public bool HasCredentials => !string.IsNullOrWhiteSpace(Login) && !string.IsNullOrWhiteSpace(Password);

    public BookGetterConfig(Options options, HttpClient client){
        Client = client;
        Login = options.Login;
        Password = options.Password;
    }

    public void Dispose() {
        Client?.Dispose();
    }
}