using System;
using Elib2Ebook.Configs;

namespace Elib2Ebook.Logic.Getters.LibSocial; 

public class HentaiLibGetter : MangaLibGetterBase {
    public HentaiLibGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://hentailib.me");
    protected override string _server => "secondary";
}