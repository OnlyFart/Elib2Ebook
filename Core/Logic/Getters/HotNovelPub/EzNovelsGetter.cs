using System;
using Core.Configs;

namespace Core.Logic.Getters.HotNovelPub; 

public class EzNovelsGetter(BookGetterConfig config) : HotNovelPubGetterBase(config) {
    protected override Uri SystemUrl => new("https://eznovels.com");
    protected override string Lang => "ru";
}