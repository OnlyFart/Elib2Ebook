using System;
using Core.Configs;

namespace Core.Logic.Getters.LibSocial;

public class MangaLibOrgGetter(BookGetterConfig config) : MangaLibGetterBase(config) {
    protected override Uri ImagesHost => new("https://img33.imgslib.link/");

    protected override Uri SystemUrl => new("https://mangalib.org/");
}