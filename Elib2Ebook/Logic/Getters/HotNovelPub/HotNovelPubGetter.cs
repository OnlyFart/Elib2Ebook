using System;
using Elib2Ebook.Configs;

namespace Elib2Ebook.Logic.Getters.HotNovelPub; 

public class HotNovelPubGetter : HotNovelPubGetterBase {
    public HotNovelPubGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://hotnovelpub.com/");
    protected override string Lang => "en";
}