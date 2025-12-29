using System;
using Core.Configs;

namespace Core.Logic.Getters.Rulate;

public class BllateGetter(BookGetterConfig config) : RulateGetterBase(config) {
    protected override Uri SystemUrl => new("https://bllate.org");
    protected override string Mature { get; }
}