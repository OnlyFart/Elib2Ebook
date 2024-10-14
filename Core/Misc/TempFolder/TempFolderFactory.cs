#nullable enable
using System.IO;

namespace Core.Misc.TempFolder; 

public class TempFolderFactory {
    /// <summary>
    /// Create temporary folder by path
    /// </summary>
    /// <param name="path">Temporary folder path</param>
    /// <returns>New instance of temporary folder</returns>
    public static TempFolder Create(string? path) {
        path ??= Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"{Path.GetRandomFileName()}_e2e_temp")).FullName;
        if (!Directory.Exists(path)) {
            Directory.CreateDirectory(path);
        }
            
        return new TempFolder(path);
    }
}