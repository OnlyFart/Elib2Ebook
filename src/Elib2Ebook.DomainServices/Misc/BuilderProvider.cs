using Elib2Ebook.DomainServices.Builders;
using Elib2Ebook.DomainServices.Configs;
using Microsoft.Extensions.Logging;

namespace Elib2Ebook.DomainServices.Misc;

public static class BuilderProvider
{
    public static BuilderBase Get(string format, Options options, ILogger logger)
    {
        return format.Trim().ToLower() switch
        {
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
