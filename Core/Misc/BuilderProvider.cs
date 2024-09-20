using System;
using Core.Configs;
using Core.Logic.Builders;
using Microsoft.Extensions.Logging;

namespace Core.Misc;

public static class BuilderProvider {
    public static BuilderBase Get(string format, Options options, ILogger logger) {
        return format.Trim().ToLower() switch {
            "fb2" => new Fb2Builder(options, logger),
            "epub" => new EpubBuilder(options, logger),
            "json" => new JsonBuilder(options, logger),
            "cbz" => new CbzBuilder(options, logger),
            "txt" => new TxtBuilder(options, logger),
            "json_lite" => new JsonLiteBuilder(options, logger),
            _ => throw new ArgumentException("Неизвестный формат", nameof(format))
        };
    }
}