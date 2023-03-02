using System;
using Elib2Ebook.Configs;

namespace Elib2Ebook.Logic.Getters.Rulate; 

public class MlateGetter : RulateGetterBase {
    public MlateGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://mlate.ru/");
}