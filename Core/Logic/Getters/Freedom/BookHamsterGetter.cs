using System;
using Core.Configs;

namespace Core.Logic.Getters.Freedom;

public class BookHamsterGetter : FreedomGetterBase {
    public BookHamsterGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://bookhamster.ru/");
}