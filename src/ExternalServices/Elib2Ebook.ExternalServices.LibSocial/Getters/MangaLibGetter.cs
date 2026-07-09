using Elib2Ebook.DomainServices.Configs;

namespace Elib2Ebook.ExternalServices.LibSocial.Getters;

public class MangaLibGetter(BookGetterConfig config) : MangaLibGetterBase(config)
{
    protected override Uri ImagesHost => new("https://img3.cdnlibs.org/");

    protected override Uri SystemUrl => new("https://mangalib.me/");

    protected override int SiteId => 1;
}
