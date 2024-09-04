using System;
using Core.Configs;

namespace Core.Logic.Getters.Fb2Top; 

public class LadyLibGetter : Fb2TopGetterBase {
    public LadyLibGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://ladylib.top/");
}