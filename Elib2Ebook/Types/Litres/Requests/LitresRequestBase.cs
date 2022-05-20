using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.Litres.Requests; 

public abstract class LitresRequestBase<T> where T : new() {
    [JsonPropertyName("id")] 
    public string Id { get; set; } = "data";
    
    [JsonPropertyName("func")]
    public string Func { get; set; }
    
    [JsonPropertyName("param")] 
    public T Param { get; } = new();
}