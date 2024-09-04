using System;
using Core.Configs;

namespace Core.Logic.Getters.Litnet; 

public class BooknetUaGetter : LitnetGetterBase {
    public BooknetUaGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://booknet.ua/");
}