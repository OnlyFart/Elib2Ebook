using System;
using Elib2Ebook.Configs;

namespace Elib2Ebook.Logic.Getters.HotNovelPub; 

public class LightNovelDailyGetter : HotNovelPubGetterBase {
    public LightNovelDailyGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://lightnoveldaily.com");
    protected override string Lang => "es";
}