using System;
using Core.Configs;

namespace Core.Logic.Getters.Litnet; 

public class LitnetGetter : LitnetGetterBase {
    public LitnetGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://litnet.com/");
}