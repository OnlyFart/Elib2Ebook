using Elib2Ebook.DomainServices.Configs;

namespace Elib2Ebook.ExternalServices.HotNovelPub.Getters;

public class EzNovelsGetter(BookGetterConfig config) : HotNovelPubGetterBase(config)
{
    protected override Uri SystemUrl => new("https://eznovels.com");
    protected override string Lang => "ru";
}
