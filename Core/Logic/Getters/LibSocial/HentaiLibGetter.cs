using System;
using Core.Configs;

namespace Core.Logic.Getters.LibSocial;

public class HentaiLibGetter(BookGetterConfig config) : MangaLibGetterBase(config) {
    protected override Uri ImagesHost => new("https://img3.imglib.info/");
    
    protected override Uri SystemUrl => new("https://hentailib.me/");
}