using System;
using Elib2Ebook.Configs;

namespace Elib2Ebook.Logic.Getters.RanobeLib; 

public class YaoiLibGetter : MangaLibGetterBase {
    public YaoiLibGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://yaoilib.me/");
}