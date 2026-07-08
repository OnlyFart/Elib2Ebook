using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.Litres.Types.Response;

internal class LitresPartnerSubscriptions
{
    [JsonPropertyName("subscriptions")]
    public LitresSubscription[] Subscriptions { get; set; }
}
