using System;
using Core.Configs;

namespace Core.Logic.Getters.Freedom;

public class FreedomGetter : FreedomGetterBase {
    public FreedomGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://ifreedom.su/");
}