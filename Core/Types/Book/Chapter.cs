using System.Collections.Generic;
using Core.Types.Common;

namespace Core.Types.Book; 

public class Chapter {
    /// <summary>
    /// Название части
    /// </summary>
    public string Title { get; set; }
        
    /// <summary>
    /// Контент части
    /// </summary>
    public string Content { get; set; }

    /// <summary>
    /// Номер тома
    /// </summary>
    public string VolumeNumber { get; set; }

    /// <summary>
    /// Номер главы
    /// </summary>
    public string ChapterNumber { get; set; }

    /// <summary>
    /// Изображения из части
    /// </summary>
    public IEnumerable<TempFile> Images { get; set; } = new List<TempFile>();

    /// <summary>
    /// Валидна ли часть
    /// </summary>
    public bool IsValid => !string.IsNullOrWhiteSpace(Content);
}