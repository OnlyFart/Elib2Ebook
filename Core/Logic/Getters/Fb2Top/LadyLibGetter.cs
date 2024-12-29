using System;
using Core.Configs;

namespace Core.Logic.Getters.Fb2Top; 

public class LadyLibGetter(BookGetterConfig config) : Fb2TopGetterBase(config) {
    protected override Uri SystemUrl => new("https://ladylib.top/");
}