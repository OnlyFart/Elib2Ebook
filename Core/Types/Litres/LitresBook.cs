using System.Collections.Generic;
using HtmlAgilityPack;

namespace Core.Types.Litres; 

public class LitresBook {
    public HtmlDocument Content;

    public Dictionary<string, LitresTarget> Targets = new();
}