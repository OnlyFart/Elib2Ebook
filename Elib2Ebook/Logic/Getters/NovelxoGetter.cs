using System;
using Elib2Ebook.Configs;

namespace Elib2Ebook.Logic.Getters; 

public class NovelxoGetter : NovelxoGetterBase {
    protected override Uri SystemUrl => new("https://novelxo.com");
    
    public NovelxoGetter(BookGetterConfig config) : base(config) { }
}