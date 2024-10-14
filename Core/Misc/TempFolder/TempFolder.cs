using System;
using System.IO;
using System.Linq;

namespace Core.Misc.TempFolder; 

public class TempFolder : IDisposable {
    /// <summary>
    /// Temporary's folder path
    /// </summary>
    public readonly string Path;

    /// <summary>
    /// Create new instance of temporary folder that will be deleted after dispose
    /// </summary>
    /// <param name="path">Temporary folder</param>
    public TempFolder(string path) {
        Path = path;
    }
        
    public void Dispose() {
        if (Directory.Exists(Path) && !Directory.EnumerateFileSystemEntries(Path).Any()) {
            Directory.Delete(Path, true);
        }
    }
}