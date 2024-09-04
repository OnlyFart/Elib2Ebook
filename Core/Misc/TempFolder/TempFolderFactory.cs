#nullable enable
using System.IO;

namespace Core.Misc.TempFolder; 

public class TempFolderFactory {
    /// <summary>
    /// Create temporary folder by path
    /// </summary>
    /// <param name="path">Temporary folder path</param>
    /// <param name="remove">Need remove?</param>
    /// <returns>New instance of temporary folder</returns>
    public static TempFolder Create(string? path, bool remove) {
        path ??= Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())).FullName;
        if (!Directory.Exists(path)) {
            Directory.CreateDirectory(path);
        }
            
        return new TempFolder(path, remove);
    }
}