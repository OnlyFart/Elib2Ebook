using System;
using Core.Configs;
using Core.Extensions;
using Core.Types.Renovels;
using HtmlAgilityPack;

namespace Core.Logic.Getters.Renovels; 

public class RenovelsGetter(BookGetterConfig config) : RenovelsGetterBase(config) {
    protected override Uri SystemUrl => new("https://renovels.org/");
    
    protected override string Segment => "novel";
    
    protected override HtmlDocument GetChapterAsHtml(RenovelsChapter response) {
        return response.Content.AsHtmlDoc();
    }
}