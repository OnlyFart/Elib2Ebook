using System;
using Elib2Ebook.Configs;

namespace Elib2Ebook.Logic.Getters; 

public class NovelxoRuGetter : NovelxoGetterBase {
    protected override Uri SystemUrl => new("https://ru.novelxo.com");
    
    public NovelxoRuGetter(BookGetterConfig config) : base(config) { }
}