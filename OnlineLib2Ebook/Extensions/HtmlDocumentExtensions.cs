using System;
using System.IO;
using System.Linq;
using System.Xml;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;

namespace OnlineLib2Ebook.Extensions {
    public static class HtmlDocumentExtensions {
        public static string AsString(this HtmlDocument self) {
            using var sw = new StringWriter();
            using var xw = new XmlTextWriter(sw);
            self.Save(xw);

            return sw.ToString();
        }

        public static string GetTextBySelector(this HtmlDocument doc, string selector) {
            return doc.DocumentNode.GetTextBySelector(selector);
        }
        
        public static string GetTextBySelector(this HtmlNode node, string selector) {
            return node.QuerySelector(selector)?.InnerText?.Trim().HtmlDecode();
        }

        public static HtmlDocument RemoveNodes(this HtmlDocument doc, Func<HtmlNode, bool> predicate) {
            var toRemove = doc.DocumentNode.ChildNodes.Where(predicate).ToList();
            foreach (var node in toRemove) {
                node.Remove();
            }

            return doc;
        }
    }
}