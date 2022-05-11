using System;
using System.Linq;
using System.Web;

namespace Elib2Ebook.Extensions; 

public static class UriExtension {
    public static string GetFileName(this Uri self) {
        return self.Segments.Last().Split(":")[0].TrimEnd('/');
    }
    
    public static string GetQueryParameter(this Uri self, string name) {
        return HttpUtility.ParseQueryString(self.Query)[name];
    }
    
    public static Uri ReplaceHost(this Uri self, string newHost) {
        var builder = new UriBuilder(self);
        builder.Host = newHost;
        return builder.Uri;
    }
}