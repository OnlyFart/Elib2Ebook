using System;
using Core.Configs;

namespace Core.Logic.Getters.Novelxo; 

public class NovelxoRuGetter(BookGetterConfig config) : NovelxoGetterBase(config) {
    protected override Uri SystemUrl => new("https://ru.novelxo.com");
}