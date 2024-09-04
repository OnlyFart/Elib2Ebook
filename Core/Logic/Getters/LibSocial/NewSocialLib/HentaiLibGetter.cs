using System;
using Core.Configs;

namespace Core.Logic.Getters.LibSocial.NewSocialLib;

public class HentaiLibGetter : MangalibLibGetterBase {
    public HentaiLibGetter(BookGetterConfig config) : base(config) { }
    
    protected override Uri ImagesHost => new("https://img3.imglib.info/");
    
    protected override Uri SystemUrl => new("https://hentailib.me/");
}