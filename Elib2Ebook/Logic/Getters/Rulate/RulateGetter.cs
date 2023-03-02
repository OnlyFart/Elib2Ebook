using System;
using Elib2Ebook.Configs;

namespace Elib2Ebook.Logic.Getters.Rulate; 

public class RulateGetter : RulateGetterBase {
    public RulateGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://tl.rulate.ru");
}