using System.Text;
using HtmlAgilityPack;

namespace Elib2Ebook.Extensions; 

public static class StringBuilderExtensions {
    /// <summary>
    /// Конвертация строки в Html документ
    /// </summary>
    /// <param name="self"></param>
    /// <returns></returns>
    public static HtmlDocument AsHtmlDoc(this StringBuilder self) {
        return self.ToString().AsHtmlDoc();
    }
}