using System;
using System.Collections.Generic;
using Core.Types.Common;

namespace Core.Types.Book; 

public class Book {
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

    public Book(Uri url) {
        Url = url;
    }
}