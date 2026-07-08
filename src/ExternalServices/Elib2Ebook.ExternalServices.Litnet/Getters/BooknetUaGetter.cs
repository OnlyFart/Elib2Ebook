using Elib2Ebook.DomainServices.Configs;

namespace Elib2Ebook.ExternalServices.Litnet.Getters;

public class BooknetUaGetter(BookGetterConfig config) : LitnetGetterBase(config)
{
    protected override Uri SystemUrl => new("https://booknet.ua/");
}
