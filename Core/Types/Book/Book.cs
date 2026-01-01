using System;
using System.Linq;
using System.Collections.Generic;
using Core.Types.Common;

namespace Core.Types.Book; 

public class Book : IDisposable {
    /// <summary>
    /// Название книги
    /// </summary>
    public string Title { get; set; }
        
    /// <summary>
    /// Автор книги
    /// </summary>
    public Author Author { get; set; }

    /// <summary>
    /// Автор книги
    /// </summary>
    public IEnumerable<Author> CoAuthors { get; set; } = new List<Author>();

    /// <summary>
    /// Описание книги
    /// </summary>
    public string Annotation { get; set; }
        
    /// <summary>
    /// Обложка
    /// </summary>
    public TempFile Cover { get; set; }
    
    /// <summary>
    /// Серия
    /// </summary>
    public Seria Seria { get; set; }

    /// <summary>
    /// Части
    /// </summary>
    public IEnumerable<Chapter> Chapters { get; set; } = new List<Chapter>();

/// Тома
    public IEnumerable<string> Volumes => Chapters.Select(c => c.VolumeNumber).Distinct();

    /// <summary>
    /// Url расположения книги
    /// </summary>
    public Uri Url { get; set; }

    /// <summary>
    /// Дополнительные файлы
    /// </summary>
    public AdditionalFileCollection AdditionalFiles { get; set; } = new();

    /// <summary>
    /// Язык книги
    /// </summary>
    public string Lang { get; set; } = "ru";

    public bool SupportSplitVolumes { get; set; } = false;
    public bool SupportSplitChapters { get; set; } = false;

    public bool SupportSplitting => SupportSplitVolumes || SupportSplitChapters;

    public Book(Uri url) {
        Url = url;
    }

    public void Dispose() {
        Cover?.Dispose();
        AdditionalFiles?.Dispose();
    }
}