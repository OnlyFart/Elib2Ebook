using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Core.Configs;
using Core.Types.Book;

namespace Core.Logic.Builders;

public class ShortImageConverter : JsonConverter<Image> {
    public override Image Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        return JsonSerializer.Deserialize<Image>(ref reader, options);
    }

    public override void Write(Utf8JsonWriter writer, Image value, JsonSerializerOptions options) {
        writer.WriteStartObject();
        
        if (value.Url != default) {
            writer.WriteString(nameof(value.Url), value.Url.ToString());
        }

        writer.WriteString(nameof(value.Directory), value.Directory);
        writer.WriteString(nameof(value.Name), value.Name);
        writer.WriteString(nameof(value.FilePath), value.FilePath);
        writer.WriteEndObject();
    }
}

public class ShortChapterConverter : JsonConverter<Chapter> {
    public override Chapter Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        return JsonSerializer.Deserialize<Chapter>(ref reader, options);
    }

    public override void Write(Utf8JsonWriter writer, Chapter value, JsonSerializerOptions options) {
        writer.WriteStartObject();
        
        writer.WriteString(nameof(value.Title), value.Title);
        writer.WriteBoolean(nameof(value.IsValid), value.IsValid);
        
        writer.WriteStartArray(nameof(value.Images));
        foreach (var image in value.Images) {
            JsonSerializer.Serialize(writer, image, options);
        }
        
        writer.WriteEndArray();
        writer.WriteEndObject();
    }
}

public class JsonLiteBuilder : BuilderBase {
    public JsonLiteBuilder(Options options) : base(options) { }

    protected override string Extension => "json";

    protected override async Task BuildInternal(Book book, string fileName) {
        await using var file = File.Create(fileName);
        
        var jsonSerializerOptions = new JsonSerializerOptions {
            WriteIndented = true,
            Converters = { new ShortChapterConverter(), new ShortImageConverter() }
        };

        await JsonSerializer.SerializeAsync(file, book, jsonSerializerOptions);
    }
}