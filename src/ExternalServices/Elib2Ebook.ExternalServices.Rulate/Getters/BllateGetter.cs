using Elib2Ebook.DomainServices.Configs;

namespace Elib2Ebook.ExternalServices.Rulate.Getters;

public class BllateGetter(BookGetterConfig config) : RulateGetterBase(config)
{
    protected override Uri SystemUrl => new("https://bllate.org");
    protected override string Mature { get; }
}
