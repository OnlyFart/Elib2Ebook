using Core.Configs;
using Core.Logic.Builders;
using Microsoft.Extensions.Logging.Abstractions;

namespace Core.Tests.Logic.Builders;

public class BuilderTests
{
    private static Options Opt(string format)
        => new(
            new[]
            {
                "https://example.com/book"
            })
        {
            Format = new[]
            {
                format
            },
            BookNamePattern = "{Book.Title}",
            Timeout = 120
        };

    public static IEnumerable<object[]> BuilderData()
    {
        yield return ["fb2", typeof(Fb2Builder)];
        yield return ["epub", typeof(EpubBuilder)];
        yield return ["cbz", typeof(CbzBuilder)];
        yield return ["txt", typeof(TxtBuilder)];
        yield return ["json", typeof(JsonBuilder)];
        yield return ["json_lite", typeof(JsonLiteBuilder)];
    }

    [Theory]
    [MemberData(nameof(BuilderData))]
    public void CanCreate(string format, Type type)
    {
        var b = (BuilderBase)Activator.CreateInstance(type, Opt(format), NullLogger.Instance)!;
        Assert.IsAssignableFrom<BuilderBase>(b);
    }
}
