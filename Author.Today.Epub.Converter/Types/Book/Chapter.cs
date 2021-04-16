using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Author.Today.Epub.Converter.Types.Book {
    public class Chapter {
        [JsonPropertyName("id")]
        public long Id { get; set; }
        
        [JsonPropertyName("title")]
        public string Title { get; set; }
        
        public string Content { get; set; }

        public Uri Path { get; set; }

        public bool IsValid => !string.IsNullOrWhiteSpace(Content);

        public List<Image> Images = new List<Image>();
    }
}
