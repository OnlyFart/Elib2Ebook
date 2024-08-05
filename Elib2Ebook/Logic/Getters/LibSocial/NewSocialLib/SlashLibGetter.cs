using System;
using Elib2Ebook.Configs;

namespace Elib2Ebook.Logic.Getters.LibSocial.NewSocialLib; 

public class SlashLibGetter : MangaLibGetter {
    public SlashLibGetter(BookGetterConfig config) : base(config) { }
    
    protected override Uri SystemUrl => new("https://v2.slashlib.me/");
}