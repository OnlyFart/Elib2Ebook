using System.Collections.Generic;
using System.IO;
using System.IO.Enumeration;
using System.Linq;
using Microsoft.Extensions.FileProviders;

namespace Elib2Ebook.Extensions;

public static class FileProviderExtensions
{
    public static byte[] ReadAllBytes(this IFileInfo fileInfo)
    {
        using var ms = new MemoryStream();
        using var source = fileInfo.CreateReadStream();
        source.CopyTo(ms);
        return ms.ToArray();
    }
    
    public static string ReadAllText(this IFileInfo fileInfo)
    {
        using var source = fileInfo.CreateReadStream();
        using var sr = new StreamReader(source);
        return sr.ReadToEnd();
    }

    public static string ReadAllText(this IFileProvider fileProvider, string path)
    {
        var file = fileProvider.GetFileInfo(path);
        return file.ReadAllText();
    }
    
    public static IEnumerable<IFileInfo> GetFiles(this IFileProvider fileProvider, string directory, string searchPattern)
    {
        return fileProvider.GetDirectoryContents(directory)
            .Where(file => FileSystemName.MatchesSimpleExpression(searchPattern, file.Name));
    }
}