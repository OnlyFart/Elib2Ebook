using System;
using System.Linq;
using System.Web;

namespace Elib2Ebook.Extensions; 

public static class UriExtension {
    public static string GetFileName(this Uri self) {
        return self.Segments.Last().TrimEnd('/');
    }
    
    public static string GetQueryParameter(this Uri self, string name) {
        return HttpUtility.ParseQueryString(self.Query)[name];
    }
}