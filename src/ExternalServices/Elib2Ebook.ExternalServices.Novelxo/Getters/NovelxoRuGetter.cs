using Elib2Ebook.DomainServices.Configs;

namespace Elib2Ebook.ExternalServices.Novelxo.Getters;

public class NovelxoRuGetter(BookGetterConfig config) : NovelxoGetterBase(config)
{
    protected override Uri SystemUrl => new("https://ru.novelxo.com");
}
