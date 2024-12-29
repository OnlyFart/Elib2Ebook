using System;
using Core.Configs;

namespace Core.Logic.Getters.LibSocial; 

public class SlashLibGetter(BookGetterConfig config) : MangaLibGetter(config) {
    protected override Uri SystemUrl => new("https://v2.slashlib.me/");
}