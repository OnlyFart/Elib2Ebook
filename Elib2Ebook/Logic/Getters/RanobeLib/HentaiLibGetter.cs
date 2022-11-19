using System;
using Elib2Ebook.Configs;
using Elib2Ebook.Types.MangaLib;
using Elib2Ebook.Types.RanobeLib;

namespace Elib2Ebook.Logic.Getters.RanobeLib; 

public class HentaiLibGetter : MangaLibGetterBase {
    public HentaiLibGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://hentailib.me");
}