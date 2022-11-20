using System;
using Elib2Ebook.Configs;

namespace Elib2Ebook.Logic.Getters.Fb2Top; 

public class LadyLibGetter : Fb2TopGetterBase {
    public LadyLibGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://ladylib.top/");
}