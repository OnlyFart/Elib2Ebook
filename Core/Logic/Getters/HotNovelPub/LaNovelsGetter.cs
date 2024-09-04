using System;
using Core.Configs;

namespace Core.Logic.Getters.HotNovelPub; 

public class LaNovelsGetter : HotNovelPubGetterBase {
    public LaNovelsGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://lanovels.com/");
    protected override string Lang => "pt";
}