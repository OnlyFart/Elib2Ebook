using System;
using Core.Configs;

namespace Core.Logic.Getters.Freedom;

public class FreedomGetter(BookGetterConfig config) : FreedomGetterBase(config) {
    protected override Uri SystemUrl => new("https://ifreedom.su/");
}