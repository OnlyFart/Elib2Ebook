using System;
using System.IO;
using System.Linq;
using System.Xml;
using HtmlAgilityPack;

namespace OnlineLib2Ebook.Extensions {
    public static class HtmlDocumentExtensions {
        public static string AsString(this HtmlDocument self) {
            using var sw = new StringWriter();
            using var xw = new XmlTextWriter(sw);
            self.Save(xw);

            return sw.ToString();
        }

        public static HtmlNode GetByFilter(this HtmlDocument doc, string name, string @class) {
            return doc.DocumentNode.GetByFilter(name, @class);
        }
        
        public static HtmlNode GetByFilter(this HtmlDocument node, string name) {
            return node.DocumentNode.Descendants()
                .FirstOrDefault(t => t.Name == name);
        }
        
        public static HtmlNode GetByFilter(this HtmlNode node, string name, string @class) {
            return node.Descendants()
                .FirstOrDefault(t => t.Name == name && t.Attributes["class"]?.Value == @class);
        }

        public static HtmlNode GetByFilterContains(this HtmlNode node, string name, string @class) {
            return node.Descendants()
                .FirstOrDefault(t => t.Name == name && t.Attributes["class"]?.Value?.Contains(@class) == true);
        }
        
        public static HtmlNode GetByFilter(this HtmlNode node, string name) {
            return node.Descendants()
                .FirstOrDefault(t => t.Name == name);
        }

        public static string GetTextByFilter(this HtmlDocument doc, string name, string @class) {
            return doc.GetByFilter(name, @class)
                ?.InnerText?
                .Trim();
        }
        
        public static string GetTextByFilter(this HtmlDocument doc, string name) {
            return doc.GetByFilter(name)?.InnerText?.Trim();
        }
        
        public static string GetTextByFilter(this HtmlNode node, string name, string @class) {
            return node.GetByFilter(name, @class)
                ?.InnerText?
                .Trim();
        }

        public static HtmlDocument RemoveNodes(this HtmlDocument doc, Func<HtmlNode, bool> predicate) {
            var toRemove = doc.DocumentNode.ChildNodes.Where(predicate).ToList();
            foreach (var node in toRemove) {
                node.Remove();
            }

            return doc;
        }
        
        public static string GetTextByFilter(this HtmlNode node, string name) {
            return node.GetByFilter(name)?.InnerText?.Trim();
        }

        public static string GetAttributeByNameAttribute(this HtmlDocument doc, string name, string attribute) {
            return doc.DocumentNode.Descendants().FirstOrDefault(t => t.Attributes["name"]?.Value == name)
                ?.Attributes[attribute]?.Value;
        }
    }
}