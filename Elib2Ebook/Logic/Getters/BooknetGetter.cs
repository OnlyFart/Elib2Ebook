using System;
using Elib2Ebook.Configs;

namespace Elib2Ebook.Logic.Getters; 

public class BooknetGetter : LitnetGetterBase {
    public BooknetGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://booknet.com/");
}