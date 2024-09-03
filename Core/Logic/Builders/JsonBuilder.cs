using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Core.Configs;
using Core.Types.Book;

namespace Core.Logic.Builders; 

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