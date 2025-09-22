using System;
using Core.Configs;

namespace Core.Logic.Getters.LibSocial;

public class MangaLibGetter(BookGetterConfig config) : MangaLibGetterBase(config) {
    protected override Uri ImagesHost => new("https://img33.imgslib.link/");

    protected override Uri SystemUrl => new("https://mangalib.me/");

    protected override int SiteId => 1;
}
