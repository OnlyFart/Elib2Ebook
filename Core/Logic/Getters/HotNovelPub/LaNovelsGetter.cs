using System;
using Core.Configs;

namespace Core.Logic.Getters.HotNovelPub; 

public class LaNovelsGetter(BookGetterConfig config) : HotNovelPubGetterBase(config) {
    protected override Uri SystemUrl => new("https://lanovels.com/");
    protected override string Lang => "pt";
}