using System;
using Core.Configs;

namespace Core.Logic.Getters.HotNovelPub; 

public class LightNovelDailyGetter : HotNovelPubGetterBase {
    public LightNovelDailyGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://lightnoveldaily.com");
    protected override string Lang => "es";
}