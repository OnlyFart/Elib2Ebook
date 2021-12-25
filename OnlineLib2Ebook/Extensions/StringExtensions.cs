using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using HtmlAgilityPack;

namespace OnlineLib2Ebook.Extensions {
    public static class StringExtensions {
        /// <summary>
        /// Конвертация строки в Html документ
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        public static HtmlDocument AsHtmlDoc(this string self) {
            var doc = new HtmlDocument();
            doc.LoadHtml(self);
            return doc;
        }

        /// <summary>
        /// Конвертация строки в Xhtml документ
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        public static HtmlDocument AsXHtmlDoc(this string self) {
            HtmlNode.ElementsFlags.Remove("style");
            HtmlNode.ElementsFlags.Remove("title");

            var doc = new HtmlDocument {
                OptionFixNestedTags = true,
                OptionAutoCloseOnEnd = true,
                OptionOutputAsXml = true,
            };

            doc.LoadHtml(self);
            return doc;
        }

        /// <summary>
        /// Удаление из строки запрещенных символов для пути
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        public static string RemoveInvalidChars(this string self) {
            var sb = new StringBuilder(self);
            foreach (var invalidFileNameChar in Path.GetInvalidFileNameChars().Union(new[]{'"'})) {
                sb.Replace(invalidFileNameChar, ' ');
            }

            return sb.ToString();
        }

        /// <summary>
        /// Обертывание строки кавычками
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        public static string CoverQuotes(this string self) {
            return "\"" + self + "\"";
        }
        
        /// <summary>
        /// Обрезка строки
        /// </summary>
        /// <param name="self"></param>
        /// <param name="lenght"></param>
        /// <returns></returns>
        public static string Crop(this string self, int lenght) {
            return self.Length > lenght ? self[..lenght] : self;
        }

        /// <summary>
        /// HtmlDecode
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        public static string HtmlDecode(this string self) {
            return HttpUtility.HtmlDecode(self);
        }
        
        /// <summary>
        /// HtmlEncode
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        public static string HtmlEncode(this string self) {
            return HttpUtility.HtmlEncode(self);
        }
    }
}