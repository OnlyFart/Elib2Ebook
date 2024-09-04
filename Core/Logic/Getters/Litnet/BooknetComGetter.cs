using System;
using Core.Configs;

namespace Core.Logic.Getters.Litnet; 

public class BooknetComGetter : LitnetGetterBase {
    public BooknetComGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://booknet.com/");
}