using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using HtmlAgilityPack;

namespace Author.Today.Epub.Converter.Extensions {
    public static class HtmlDocumentExtensions {
        public static string AsString(this HtmlDocument self) {
            var sb = new StringBuilder(); 
            using var stringWriter = new StringWriter(sb);
            using var xmlWriter = new XmlTextWriter(stringWriter);
            self.Save(xmlWriter);

            return sb.ToString();
        }

        public static HtmlNode GetByFilter(this HtmlDocument doc, string name, string @class){
            return doc.DocumentNode.Descendants()
                .FirstOrDefault(t => t.Name == name && t.Attributes["class"]?.Value == @class);
        }
        
        public static string GetTextByFilter(this HtmlDocument doc, string name, string @class) {
            return doc.GetByFilter(name, @class)
                ?.InnerText?
                .Trim();
        }
        
        public static string GetAttributeByNameAttribute(this HtmlDocument doc, string name, string attribute){
            return doc.DocumentNode.Descendants().FirstOrDefault(t => t.Attributes["name"]?.Value == name)?.Attributes[attribute]?.Value;
        }
    }
}
