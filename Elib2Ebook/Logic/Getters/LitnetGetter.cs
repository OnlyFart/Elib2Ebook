using System;
using Elib2Ebook.Configs;

namespace Elib2Ebook.Logic.Getters; 

public class LitnetGetter : LitnetGetterBase {
    public LitnetGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://litnet.com/");
}