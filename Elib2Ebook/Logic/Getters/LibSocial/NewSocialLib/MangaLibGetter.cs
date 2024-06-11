using System;
using Elib2Ebook.Configs;

namespace Elib2Ebook.Logic.Getters.LibSocial.NewSocialLib;

public class MangaLibGetter : MangalibLibGetterBase {
    public MangaLibGetter(BookGetterConfig config) : base(config) { }

    protected override Uri ImagesHost => new("https://img33.imgslib.link/");

    protected override Uri SystemUrl => new("https://test-front.mangalib.me/");
}