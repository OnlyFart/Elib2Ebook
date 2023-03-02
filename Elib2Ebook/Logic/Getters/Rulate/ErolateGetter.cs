using System;
using System.Net;
using System.Threading.Tasks;
using Elib2Ebook.Configs;
using Elib2Ebook.Extensions;
using Elib2Ebook.Types.Rulate;
using HtmlAgilityPack;

namespace Elib2Ebook.Logic.Getters.Rulate; 

public class ErolateGetter : RulateGetterBase {
    public ErolateGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://erolate.com/");
    public override async Task Init() {
        await base.Init();
        Config.Client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
        Config.CookieContainer.Add(SystemUrl, new Cookie("mature", "7da3ee594b38fc5355692d978fe8f5adbeb3d17di%3A1%3B"));
    }
}