namespace Elib2Ebook.Domain.Book;

public class Seria
{
    /// <summary>
    /// Название серии
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Номер в серии
    /// </summary>
    public string Number { get; set; }

    /// <summary>
    /// Ссылка на книги серии
    /// </summary>
    public Uri Url { get; set; }
}
