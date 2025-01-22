using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Core.Configs;
using Core.Types.Book;
using Microsoft.Extensions.Logging;

namespace Core.Logic.Builders; 

public class JsonBuilder(Options options, ILogger logger) : BuilderBase(options, logger) {
    protected override string Extension => "json";

    protected override async Task BuildInternal(Book book, string fileName) {
        await using var file = File.Create(fileName);
        
        var jsonSerializerOptions = new JsonSerializerOptions {
            WriteIndented = true
        };
        
        await JsonSerializer.SerializeAsync(file, book, jsonSerializerOptions);
    }
}