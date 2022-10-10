using System.Text.Json.Serialization;
using Elib2Ebook.Types.Dreame;

namespace Elib2Ebook.Logic.Getters; 

public class DreameCatalog {
    [JsonPropertyName("pager")]
    public DreamePager Pager { get; set; }
}