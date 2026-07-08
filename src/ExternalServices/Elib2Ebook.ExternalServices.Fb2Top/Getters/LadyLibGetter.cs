using Elib2Ebook.DomainServices.Configs;

namespace Elib2Ebook.ExternalServices.Fb2Top.Getters;

public class LadyLibGetter(BookGetterConfig config) : Fb2TopGetterBase(config)
{
    protected override Uri SystemUrl => new("https://ladylib.top/");
}
