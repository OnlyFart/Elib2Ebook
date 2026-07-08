using Elib2Ebook.DomainServices.Configs;

namespace Elib2Ebook.ExternalServices.Freedom.Getters;

public class FreedomGetter(BookGetterConfig config) : FreedomGetterBase(config)
{
    protected override Uri SystemUrl => new("https://ifreedom.su/");
}
