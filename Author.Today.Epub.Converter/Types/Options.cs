using CommandLine;

namespace Author.Today.Epub.Converter.Types {
    public class Options {
        [Option("id", Required = true, HelpText = "Идентификатор книги")]
        public long BookId { get; set; }
        
        [Option("proxy", Required = false, HelpText = "Прокси в формате <host>:<port>", Default = "")]
        public string Proxy { get; set; }
        
        [Option("save", Required = false, HelpText = "Директория для сохранения книги")]
        public string SavePath { get; set; }
    }
}
