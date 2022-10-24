using System;
using Elib2Ebook.Configs;

namespace Elib2Ebook.Logic.Getters.Litnet; 

public class BooknetUaGetter : LitnetGetterBase {
    public BooknetUaGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://booknet.ua/");
}