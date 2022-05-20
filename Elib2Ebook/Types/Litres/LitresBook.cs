using System.Collections.Generic;
using HtmlAgilityPack;

namespace Elib2Ebook.Types.Litres; 

public class LitresBook {
    public HtmlDocument Content;

    public Dictionary<string, LitresTarget> Targets = new();
}