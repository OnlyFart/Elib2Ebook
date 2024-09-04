using System;

namespace Core.Types.Book; 

public class Author {
    /// <summary>
    /// Имя автора
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Url профиля автора
    /// </summary>
    public Uri Url { get; set; }

    public Author(string name, Uri url = null) {
        Name = name;
        Url = url;
    }
}