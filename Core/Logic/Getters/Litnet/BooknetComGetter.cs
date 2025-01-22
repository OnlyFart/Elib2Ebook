using System;
using Core.Configs;

namespace Core.Logic.Getters.Litnet; 

public class BooknetComGetter(BookGetterConfig config) : LitnetGetterBase(config) {
    protected override Uri SystemUrl => new("https://booknet.com/");
}