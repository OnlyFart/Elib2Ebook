using System;
using Core.Configs;

namespace Core.Logic.Getters.Novelxo; 

public class NovelxoGetter : NovelxoGetterBase {
    protected override Uri SystemUrl => new("https://novelxo.com");
    
    public NovelxoGetter(BookGetterConfig config) : base(config) { }
}