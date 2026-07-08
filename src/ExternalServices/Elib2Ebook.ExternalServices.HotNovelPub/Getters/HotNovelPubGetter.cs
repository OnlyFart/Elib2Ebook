using Elib2Ebook.DomainServices.Configs;

namespace Elib2Ebook.ExternalServices.HotNovelPub.Getters;

public class HotNovelPubGetter(BookGetterConfig config) : HotNovelPubGetterBase(config)
{
    protected override Uri SystemUrl => new("https://hotnovelpub.com/");
    protected override string Lang => "en";
}
