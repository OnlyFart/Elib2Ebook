using System;
using Elib2Ebook.Configs;

namespace Elib2Ebook.Logic.Getters.Litnet; 

public class LitnetGetter : LitnetGetterBase {
    public LitnetGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://litnet.com/");
    protected override Uri ApiIp => new("https://185.175.45.123/");
}