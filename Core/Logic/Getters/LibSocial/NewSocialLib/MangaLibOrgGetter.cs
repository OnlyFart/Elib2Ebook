using System;
using Core.Configs;

namespace Core.Logic.Getters.LibSocial.NewSocialLib;

public class MangaLibOrgGetter : MangaLibGetterBase {
    public MangaLibOrgGetter(BookGetterConfig config) : base(config) { }

    protected override Uri ImagesHost => new("https://img33.imgslib.link/");

    protected override Uri SystemUrl => new("https://mangalib.org/");
}