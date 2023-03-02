using System;
using Elib2Ebook.Configs;

namespace Elib2Ebook.Logic.Getters.Rulate; 

public class RulateGetter : RulateGetterBase {
    public RulateGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://tl.rulate.ru");
    protected override string Mature => "c3a2ed4b199a1a15f5a5483504c7a75a7030dc4bi%3A1%3B";
}