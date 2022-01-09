using System;
using System.Linq;

namespace OnlineLib2Ebook.Extensions; 

public static class UriExtension {
    public static string GetFileName(this Uri self) {
        return self.Segments.Last().TrimEnd('/');
    }
}