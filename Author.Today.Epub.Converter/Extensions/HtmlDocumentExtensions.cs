using System.IO;
using System.Linq;
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

        public static string GetFirstOrDefault(this HtmlDocument doc, string name, string @class) {
            return doc.DocumentNode.Descendants()
                .FirstOrDefault(t => t.Name == name && t.Attributes["class"]?.Value == @class)
                ?.InnerText?
                .Trim();
        }
    }
}
