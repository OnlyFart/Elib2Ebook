using System;
using Core.Configs;

namespace Core.Logic.Getters.HotNovelPub; 

public class LightNovelDailyGetter(BookGetterConfig config) : HotNovelPubGetterBase(config) {
    protected override Uri SystemUrl => new("https://lightnoveldaily.com");
    protected override string Lang => "es";
}