using System;
using Elib2Ebook.Configs;

namespace Elib2Ebook.Logic.Getters.HotNovelPub; 

public class EzNovelsGetter : HotNovelPubGetterBase {
    public EzNovelsGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://eznovels.com");
    protected override string Lang => "ru";
}