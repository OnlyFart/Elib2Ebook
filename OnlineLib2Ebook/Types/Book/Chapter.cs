using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OnlineLib2Ebook.Types.Book {
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
        /// Изображения из части
        /// </summary>
        public IEnumerable<Image> Images { get; set; }

        /// <summary>
        /// Валидна ли часть
        /// </summary>
        public bool IsValid => !string.IsNullOrWhiteSpace(Content);
    }
}
