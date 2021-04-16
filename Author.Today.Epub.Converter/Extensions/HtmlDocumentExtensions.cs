using System.IO;
using System.Text;
using HtmlAgilityPack;

namespace Author.Today.Epub.Converter.Extensions {
    public static class HtmlDocumentExtensions {
        public static string AsString(this HtmlDocument self) {
            var sb = new StringBuilder(); 
            using var stringWriter = new StringWriter(sb);
            self.Save(stringWriter);

            return sb.ToString();
        }
    }
}
