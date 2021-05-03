using System;
using System.Linq;

namespace Author.Today.Epub.Converter.Extensions {
    public static class UriExtension {
        public static string GetFileName(this Uri self) {
            return self.Segments.Last().TrimEnd('/');
        }
    }
}