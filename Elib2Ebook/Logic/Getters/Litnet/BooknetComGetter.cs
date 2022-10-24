using System;
using Elib2Ebook.Configs;

namespace Elib2Ebook.Logic.Getters.Litnet; 

public class BooknetComGetter : LitnetGetterBase {
    public BooknetComGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://booknet.com/");
}