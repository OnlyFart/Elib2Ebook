using System.Text.Json.Serialization;

namespace Core.Types.Litres.Response;

public class LitresMe {
    [JsonPropertyName("partner_subscriptions")]
    public LitresPartnerSubscriptions PartnerSubscriptions { get; set; }
}