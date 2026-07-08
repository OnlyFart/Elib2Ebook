using HtmlAgilityPack;

namespace Elib2Ebook.ExternalServices.Litres.Types;

internal class LitresBook
{
    public HtmlDocument Content;

    public Dictionary<string, LitresTarget> Targets = new();
}
