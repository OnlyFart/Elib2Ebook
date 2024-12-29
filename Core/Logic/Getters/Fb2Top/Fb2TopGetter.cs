using System;
using Core.Configs;

namespace Core.Logic.Getters.Fb2Top; 

public class Fb2TopGetter(BookGetterConfig config) : Fb2TopGetterBase(config) {
    protected override Uri SystemUrl => new("https://fb2.top/");
}