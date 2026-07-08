using Elib2Ebook.DomainServices.Configs;

namespace Elib2Ebook.ExternalServices.HotNovelPub.Getters;

public class LightNovelDailyGetter(BookGetterConfig config) : HotNovelPubGetterBase(config)
{
    protected override Uri SystemUrl => new("https://lightnoveldaily.com");
    protected override string Lang => "es";
}
