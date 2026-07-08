using Elib2Ebook.DomainServices.Configs;

namespace Elib2Ebook.ExternalServices.Fb2Top.Getters;

public class Fb2TopGetter(BookGetterConfig config) : Fb2TopGetterBase(config)
{
    protected override Uri SystemUrl => new("https://fb2.top/");
}
