using System;
using Core.Configs;

namespace Core.Logic.Getters.HotNovelPub; 

public class EzNovelsGetter : HotNovelPubGetterBase {
    public EzNovelsGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://eznovels.com");
    protected override string Lang => "ru";
}