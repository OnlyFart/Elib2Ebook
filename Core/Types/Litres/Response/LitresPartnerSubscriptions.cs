using System.Text.Json.Serialization;

namespace Core.Types.Litres.Response;

public class LitresPartnerSubscriptions {
    [JsonPropertyName("subscriptions")]
    public LitresSubscription[] Subscriptions { get; set; }
}