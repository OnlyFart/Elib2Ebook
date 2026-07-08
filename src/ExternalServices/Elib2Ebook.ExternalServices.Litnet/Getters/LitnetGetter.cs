using Elib2Ebook.DomainServices.Configs;

namespace Elib2Ebook.ExternalServices.Litnet.Getters;

public class LitnetGetter(BookGetterConfig config) : LitnetGetterBase(config)
{
    protected override Uri SystemUrl => new("https://litnet.com/");
}
