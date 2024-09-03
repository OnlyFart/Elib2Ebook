using System;
using Core.Configs;

namespace Core.Logic.Getters.HotNovelPub; 

public class HotNovelPubGetter : HotNovelPubGetterBase {
    public HotNovelPubGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://hotnovelpub.com/");
    protected override string Lang => "en";
}