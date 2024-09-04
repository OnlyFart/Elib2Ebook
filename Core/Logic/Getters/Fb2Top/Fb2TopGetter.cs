using System;
using Core.Configs;

namespace Core.Logic.Getters.Fb2Top; 

public class Fb2TopGetter : Fb2TopGetterBase {
    public Fb2TopGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://fb2.top/");
}