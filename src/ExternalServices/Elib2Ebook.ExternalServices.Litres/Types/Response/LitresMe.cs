using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.Litres.Types.Response;

internal class LitresMe
{
    [JsonPropertyName("partner_subscriptions")]
    public LitresPartnerSubscriptions PartnerSubscriptions { get; set; }
}
