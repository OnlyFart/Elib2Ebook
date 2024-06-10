using System;
using Elib2Ebook.Configs;

namespace Elib2Ebook.Logic.Getters.LibSocial.OldSocialLib; 

public class YaoiOldLibGetter : MangaOldLibGetterBase {
    public YaoiOldLibGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://yaoilib.me/");
}