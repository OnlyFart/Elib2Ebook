using Elib2Ebook.DomainServices.Configs;

namespace Elib2Ebook.ExternalServices.HotNovelPub.Getters;

public class LaNovelsGetter(BookGetterConfig config) : HotNovelPubGetterBase(config)
{
    protected override Uri SystemUrl => new("https://lanovels.com/");
    protected override string Lang => "pt";
}
