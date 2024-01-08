using System;
using Elib2Ebook.Configs;

namespace Elib2Ebook.Logic.Getters.Freedom;

public class FreedomGetter : FreedomGetterBase {
    public FreedomGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://ifreedom.su/");
}