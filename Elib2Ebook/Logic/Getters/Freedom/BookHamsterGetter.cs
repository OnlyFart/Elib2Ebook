using System;
using Elib2Ebook.Configs;

namespace Elib2Ebook.Logic.Getters.Freedom;

public class BookHamsterGetter : FreedomGetterBase {
    public BookHamsterGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://bookhamster.ru/");
}