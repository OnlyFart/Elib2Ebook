using System;
using Core.Configs;

namespace Core.Logic.Getters.Novelxo; 

public class NovelxoGetter(BookGetterConfig config) : NovelxoGetterBase(config) {
    protected override Uri SystemUrl => new("https://novelxo.com");
}