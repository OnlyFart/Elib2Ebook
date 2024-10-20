using System.Collections.Generic;
using System.Linq;
using CommandLine;
using Core.Misc;

namespace Core.Configs; 

public class Options {
    public Options() { }

    public Options(IEnumerable<string> url) {
        Url = url;
    }

    [Option('u', "url", Required = true, HelpText = "Ссылка на книгу", Separator = ',')]
    public IEnumerable<string> Url { get; set; }
    
    [Option('f', "format", Required = true, HelpText = "Формат для сохранения книги. Допустимые значения: epub, fb2, cbz, json", Separator = ',')]
    public IEnumerable<string> Format { get; set; }
    
    [Option('l', "login", Required = false, HelpText = "Логин от системы")]
    public string Login { get; set; }
        
    [Option('p', "password", Required = false, HelpText = "Пароль от системы")]
    public string Password { get; set; }
        
    [Option("proxy", Required = false, HelpText = "Прокси в формате <host>:<port>")]
    public string Proxy { get; set; }
        
    [Option('s', "save", Required = false, HelpText = "Директория для сохранения книги")]
    public string SavePath { get; set; }

    [Option('c', "cover", Required = false, HelpText = "Сохранить обложку книги в отдельный файл")]
    public bool Cover { get; set; }
    
    [Option('t', "timeout", Required = false, HelpText = "Timeout для запросов в секундах", Default = 120)]
    public int Timeout { get; set; }
    
    [Option('d', "delay", Required = false, HelpText = "Задержка между запросами в секундах", Default = 0)]
    public int Delay { get; set; }
    
    [Option("no-image", Required = false, HelpText = "Не загружать картинки")]
    public bool NoImage { get; set; }

    [Option("start", Required = false, HelpText = "Стартовый номер главы")]
    public int? Start { get; set; }
    
    [Option("end", Required = false, HelpText = "Конечный номер главы")]
    public int? End { get; set; }

    [Option("start-name", Required = false, HelpText = "Стартовое название главы")]
    public string StartName { get; set; }
    
    [Option("end-name", Required = false, HelpText = "Конечное название главы")]
    public string EndName { get; set; }

    [Option("temp", Required = false, HelpText = "Папка для временного хранения картинок")]
    public string TempPath { get; set; }
    
    [Option("save-temp", Required = false, HelpText = "Сохранять временные файлы", Default = false)]
    public bool SaveTemp { get; set; }
    
    [Option("no-chapters", Required = false, HelpText = "Не загружать главы", Default = false)]
    public bool NoChapters { get; set; }
    
    [Option("additional", Required = false, HelpText = "Сохранить дополнительные файлы", Default = false)]
    public bool Additional { get; set; }

    [Option("additional-types", Required = false, HelpText = "Типы дополнительных файлов. Допустимые значения: books, audio, images", Separator = ',')]
    public IEnumerable<AdditionalTypeEnum> AdditionalTypes { get; set; }
    
    [Option("book-name-pattern", Required = false, HelpText = "Шаблон имени файла.", Default = "{Author.Name} - {Book.Title}")]
    public string BookNamePattern { get; set; }
    
    public bool HasAdditionalType(AdditionalTypeEnum type) => Additional && (AdditionalTypes == default || !AdditionalTypes.Any() || AdditionalTypes.Contains(type));

    public string ResourcesPath => "Patterns";
}