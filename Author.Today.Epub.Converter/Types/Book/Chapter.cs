using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Author.Today.Epub.Converter.Types.Book {
    public class Chapter {
        /// <summary>
        /// Идентификатор части
        /// </summary>
        [JsonPropertyName("id")]
        public long Id { get; set; }
        
        /// <summary>
        /// Название части
        /// </summary>
        [JsonPropertyName("title")]
        public string Title { get; set; }
        
        /// <summary>
        /// Контент части
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// Uri адрес части
        /// </summary>
        public Uri Path { get; set; }

        /// <summary>
        /// Валидна ли частьЫ
        /// </summary>
        public bool IsValid => !string.IsNullOrWhiteSpace(Content);

        /// <summary>
        /// Изображения из части
        /// </summary>
        public readonly List<Image> Images = new();
    }
}
