using System;
using Elib2Ebook.Configs;

namespace Elib2Ebook.Logic.Getters.LibSocial; 

public class YaoiLibGetter : MangaLibGetterBase {
    public YaoiLibGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://yaoilib.me/");
}