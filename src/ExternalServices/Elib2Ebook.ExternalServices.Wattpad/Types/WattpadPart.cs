using System.Text.Json.Serialization;
using Elib2Ebook.DomainServices.Extensions;

namespace Elib2Ebook.ExternalServices.Wattpad.Types;

internal class WattpadPart
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { private get; set; }

    public string FullName => Title.Replace("\r", " ").CollapseWhitespace().Trim();

    public Uri Url => new($"https://www.wattpad.com/apiv2/storytext?id={Id}");
}
