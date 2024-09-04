using System;
using Core.Configs;

namespace Core.Logic.Getters.LibSocial.OldSocialLib; 

public class MangaOldLibGetter : MangaOldLibGetterBase {
    public MangaOldLibGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://mangalib.me");
}