using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Elib2Ebook.Configs;
using Elib2Ebook.Types.Book;

namespace Elib2Ebook.Logic.Builders; 

public class JsonBuilder : BuilderBase {
    public JsonBuilder(Options options) : base(options) {
        
    }

    protected override string Extension => "json";

    protected override async Task BuildInternal(Book book, string fileName) {
        await using var file = File.Create(fileName);
        var jsonSerializerOptions = new JsonSerializerOptions {
            WriteIndented = true
        };
        
        await JsonSerializer.SerializeAsync(file, book, jsonSerializerOptions);
    }
}