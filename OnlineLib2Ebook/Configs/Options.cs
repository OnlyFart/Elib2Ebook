using CommandLine;

namespace OnlineLib2Ebook.Configs {
    public class Options {
        [Option("url", Required = true, HelpText = "Ссылка на книгу")]
        public string Url { get; set; }
        
        [Option("proxy", Required = false, HelpText = "Прокси в формате <host>:<port>", Default = "")]
        public string Proxy { get; set; }
        
        [Option("save", Required = false, HelpText = "Директория для сохранения книги")]
        public string SavePath { get; set; }
        
        [Option("format", Required = true, HelpText = "Формат для сохранения книги")]
        public string Format { get; set; }

        [Option("login", Required = false, HelpText = "Логин от системы")]
        public string Login { get; set; }
        
        [Option("password", Required = false, HelpText = "Пароль от системы")]
        public string Password { get; set; }
    }
}
