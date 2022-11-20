using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.FileProviders;

namespace Elib2Ebook.Logic;

public class FileProvider
{
    public static readonly IFileProvider Instance;

    static FileProvider()
    {
        var cwd = AppDomain.CurrentDomain.BaseDirectory;
        var providers = new List<IFileProvider>();
        var patternsDirectory = Path.Combine(cwd);

        if (Directory.Exists(patternsDirectory))
        {
            providers.Add(new PhysicalFileProvider(patternsDirectory));
        }
        
        providers.Add(new ManifestEmbeddedFileProvider(Assembly.GetExecutingAssembly()));

        Instance = new CompositeFileProvider(providers);
    }
}