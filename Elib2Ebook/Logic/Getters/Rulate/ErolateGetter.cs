using System;
using Elib2Ebook.Configs;

namespace Elib2Ebook.Logic.Getters.Rulate; 

public class ErolateGetter : RulateGetterBase {
    public ErolateGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://erolate.com/");
}