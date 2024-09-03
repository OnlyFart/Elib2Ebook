using System;
using Core.Configs;
using Core.Logic.Builders;

namespace Core.Misc;

public class BuilderProvider {
    public static BuilderBase Get(string format, Options options) {
        return format.Trim().ToLower() switch {
            "fb2" => new Fb2Builder(options),
            "epub" => new EpubBuilder(options),
            "json" => new JsonBuilder(options),
            "cbz" => new CbzBuilder(options),
            "txt" => new TxtBuilder(options),
            "json_lite" => new JsonLiteBuilder(options),
            _ => throw new ArgumentException("Неизвестный формат", nameof(format))
        };
    }
}