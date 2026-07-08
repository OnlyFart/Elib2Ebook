using Elib2Ebook.DomainServices.Configs;

namespace Elib2Ebook.ExternalServices.Novelxo.Getters;

public class NovelxoGetter(BookGetterConfig config) : NovelxoGetterBase(config)
{
    protected override Uri SystemUrl => new("https://novelxo.com");
}
