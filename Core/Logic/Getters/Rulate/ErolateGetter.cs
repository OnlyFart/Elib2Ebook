using System;
using Core.Configs;

namespace Core.Logic.Getters.Rulate; 

public class ErolateGetter : RulateGetterBase {
    public ErolateGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://erolate.com/");
    protected override string Mature => "7da3ee594b38fc5355692d978fe8f5adbeb3d17di%3A1%3B";
}