using System;
using Core.Configs;

namespace Core.Logic.Getters.LibSocial; 

public class SlashLibGetter : MangaLibGetter {
    public SlashLibGetter(BookGetterConfig config) : base(config) { }
    
    protected override Uri SystemUrl => new("https://v2.slashlib.me/");
}
