using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace Core.Extensions; 

public static class UriExtension {
    private static readonly IdnMapping Idn = new();
    
    public static string GetFileName(this Uri self) {
        return self.Segments.Last().Split(":")[0].TrimEnd('/');
    }

    public static Uri MakeRelativeUri(this Uri self, string relative) {
        return new(self, relative);
    }

    public static Uri AppendSegment(this Uri self, string segment) {
        return new(self.ToString().TrimEnd('/') + "/" + segment.TrimStart('/'));
    }

    public static string GetSegment(this Uri self, int index) {
        var builder = new UriBuilder {
            Scheme = self.Scheme,
            Host = self.Host,
            Path = Regex.Replace(self.AbsolutePath, "/+", "/")
        };
        
        return builder.Uri.Segments[index].Trim('/');
    }

    public static Uri AppendQueryParameter(this Uri self, string name, object value) {
        var query = HttpUtility.ParseQueryString(self.Query);
        query.Add(name, value.ToString());
        return new UriBuilder(self) {
            Query = query.ToString()!
        }.Uri;
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