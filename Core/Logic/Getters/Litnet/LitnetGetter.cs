using System;
using Core.Configs;

namespace Core.Logic.Getters.Litnet; 

public class LitnetGetter(BookGetterConfig config) : LitnetGetterBase(config) {
    protected override Uri SystemUrl => new("https://litnet.com/");
}