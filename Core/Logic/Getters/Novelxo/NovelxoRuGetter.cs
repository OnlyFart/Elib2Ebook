using System;
using Core.Configs;

namespace Core.Logic.Getters.Novelxo; 

public class NovelxoRuGetter : NovelxoGetterBase {
    protected override Uri SystemUrl => new("https://ru.novelxo.com");
    
    public NovelxoRuGetter(BookGetterConfig config) : base(config) { }
}