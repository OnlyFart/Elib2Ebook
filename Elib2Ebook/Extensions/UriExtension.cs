using System;
using System.Globalization;
using System.Linq;
using System.Web;

namespace Elib2Ebook.Extensions; 

public static class UriExtension {
    private static readonly IdnMapping Idn = new();
    
    public static string GetFileName(this Uri self) {
        return self.Segments.Last().Split(":")[0].TrimEnd('/');
    }
    
    public static string GetQueryParameter(this Uri self, string name) {
        return HttpUtility.ParseQueryString(self.Query)[name];
    }
    
    public static Uri ReplaceHost(this Uri self, string newHost) {
        return new UriBuilder(self) {
            Host = newHost
        }.Uri;
    }

    public static bool IsSameHost(this Uri self, Uri url) {
        return string.Equals(Idn.GetAscii(self.Host).Replace("www.", ""), Idn.GetAscii(url.Host).Replace("www.", ""), StringComparison.InvariantCultureIgnoreCase);
    }
    
    public static bool IsSameSubDomain(this Uri self, Uri url) {
        var urlParts = url.Host.Replace("www.", string.Empty).Split(".");
        var selfParts = self.Host.Replace("www.", string.Empty).Split(".");

        return selfParts.Length >= urlParts.Length && string.Equals(Idn.GetAscii(string.Join(".", selfParts[^urlParts.Length..])), Idn.GetAscii(string.Join(".", urlParts)), StringComparison.InvariantCultureIgnoreCase);
    }
}