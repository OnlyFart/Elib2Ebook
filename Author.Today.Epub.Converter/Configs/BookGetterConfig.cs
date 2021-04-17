using System.Net.Http;

namespace Author.Today.Epub.Converter.Configs {
    public class BookGetterConfig  {
        public HttpClient Client { get; }
        public string Pattern { get; }
        public string Login { get; }
        public string Password { get; }
        public bool HasCredentials => !string.IsNullOrWhiteSpace(Login) && !string.IsNullOrWhiteSpace(Password);
        
        public MultipartFormDataContent GenerateAuthData(string token) {
            return new() {
                {new StringContent(token), "__RequestVerificationToken"},
                {new StringContent(Login), "Login"},
                {new StringContent(Password), "Password"}
            };
        }

        public BookGetterConfig(Options options, HttpClient client, string pattern){
            Client = client;
            Pattern = pattern;
            Login = options.Login;
            Password = options.Password;
        }
    }
}