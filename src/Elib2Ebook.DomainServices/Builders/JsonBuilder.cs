using System.Text.Json;
using Elib2Ebook.Domain.Book;
using Elib2Ebook.DomainServices.Configs;
using Microsoft.Extensions.Logging;

namespace Elib2Ebook.DomainServices.Builders;

public class JsonBuilder(Options options, ILogger logger) : BuilderBase(options, logger)
{
    protected override string Extension => "json";

    protected override async Task BuildInternal(Book book, string fileName)
    {
        await using var file = File.Create(fileName);

        var jsonSerializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        await JsonSerializer.SerializeAsync(file, book, jsonSerializerOptions);
    }
}
