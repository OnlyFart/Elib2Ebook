using System.Net.Http;

namespace Author.Today.Epub.Converter.Configs {
    public class BookGetterConfig  {
        public HttpClient Client { get; }
        public string Pattern { get; }
        public string Login { get; }
        public string Password{ get; }

        public BookGetterConfig(Options options, HttpClient client, string pattern){
            Client = client;
            Pattern = pattern;
            Login = options.Login;
            Password = options.Password;
        }
    }
}