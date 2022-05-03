using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.Bookstab; 

public class BooksnabUser {
    [JsonPropertyName("pseudonym")]
    public string Pseudonym { get; set; }
}