using System.IO;
using System.Text;
using HtmlAgilityPack;

namespace Elib2Ebook.Extensions; 

public static class StreamExtension {
    public static HtmlDocument AsHtmlDoc(this Stream self) {
        var doc = new HtmlDocument();
        doc.Load(self, Encoding.UTF8);
        return doc;
    }
}