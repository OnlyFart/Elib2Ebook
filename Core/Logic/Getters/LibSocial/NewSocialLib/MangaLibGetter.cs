using System;
using Core.Configs;

namespace Core.Logic.Getters.LibSocial.NewSocialLib;

public class MangaLibGetter : MangaLibGetterBase {
    public MangaLibGetter(BookGetterConfig config) : base(config) { }

    protected override Uri ImagesHost => new("https://img33.imgslib.link/");

    protected override Uri SystemUrl => new("https://test-front.mangalib.me/");
}