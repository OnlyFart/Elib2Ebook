using Elib2Ebook.DomainServices.Configs;

namespace Elib2Ebook.ExternalServices.LibSocial.Getters;

public class MangaLibOrgGetter(BookGetterConfig config) : MangaLibGetterBase(config)
{
    protected override Uri ImagesHost => new("https://img33.imgslib.link/");

    protected override Uri SystemUrl => new("https://mangalib.org/");

    protected override int SiteId => 1;
}
