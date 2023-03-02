using System;
using System.Net;
using System.Threading.Tasks;
using Elib2Ebook.Configs;
using Elib2Ebook.Extensions;
using Elib2Ebook.Types.Rulate;
using HtmlAgilityPack;

namespace Elib2Ebook.Logic.Getters.Rulate; 

public class MlateGetter : RulateGetterBase {
    public MlateGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://mlate.ru/");
    public override async Task Init() {
        await base.Init();
        Config.Client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
        Config.CookieContainer.Add(SystemUrl, new Cookie("mature", "c3a2ed4b199a1a15f5a5483504c7a75a7030dc4bi%3A1%3B"));
    }
}