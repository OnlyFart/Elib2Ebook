using Elib2Ebook.DomainServices.Configs;

namespace Elib2Ebook.ExternalServices.Rulate.Getters;

public class MlateGetter(BookGetterConfig config) : RulateGetterBase(config)
{
    protected override Uri SystemUrl => new("https://mlate.ru/");
    protected override string Mature => "c3a2ed4b199a1a15f5a5483504c7a75a7030dc4bi%3A1%3B";
}
