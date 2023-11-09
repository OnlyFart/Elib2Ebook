using System;
using Elib2Ebook.Configs;

namespace Elib2Ebook.Logic.Getters.LibSocial; 

public class MangaLibGetter : MangaLibGetterBase {
    public MangaLibGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://mangalib.me");
}