using System;

namespace Elib2Ebook.Types.Book; 

public class Author {
    /// <summary>
    /// Имя автора
    /// </summary>
    public string Name;

    /// <summary>
    /// Url профиля автора
    /// </summary>
    public Uri Url;

    public Author(string name, Uri url = null) {
        Name = name;
        Url = url;
    }
}