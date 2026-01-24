using System.Text.Json.Serialization;

namespace Core.Types.Litres.Response;
public class LitresWebAuthPayloadData
{
    [JsonPropertyName("sid")]
    public string Sid { get; set; }
}

public class LitresWebAuthPayload
{
    [JsonPropertyName("data")]
    public LitresWebAuthPayloadData Data { get; set; }
}

public class LitresWebAuthResponse
{
    [JsonPropertyName("status")]
    public int Status { get; set; }
    
    [JsonPropertyName("payload")]
    public LitresWebAuthPayload Payload { get; set; }

    [JsonPropertyName("error")]
    public string Error { get; set; } = "";
}

