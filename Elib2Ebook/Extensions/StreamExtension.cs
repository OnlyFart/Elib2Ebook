using System.IO;
using System.Text;
using HtmlAgilityPack;

namespace Elib2Ebook.Extensions; 

public static class StreamExtension {
    public static HtmlDocument AsHtmlDoc(this Stream self, Encoding encoding = null) {
        var doc = new HtmlDocument();
        doc.Load(self, encoding ?? Encoding.UTF8);
        return doc;
    }
}