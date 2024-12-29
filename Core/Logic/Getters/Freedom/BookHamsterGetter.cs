using System;
using Core.Configs;

namespace Core.Logic.Getters.Freedom;

public class BookHamsterGetter(BookGetterConfig config) : FreedomGetterBase(config) {
    protected override Uri SystemUrl => new("https://bookhamster.ru/");
}