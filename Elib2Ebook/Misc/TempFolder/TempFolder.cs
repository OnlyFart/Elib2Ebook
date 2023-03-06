using System;
using System.IO;

namespace Elib2Ebook.Misc.TempFolder; 

public class TempFolder : IDisposable {
    /// <summary>
    /// Temporary's folder path
    /// </summary>
    public readonly string Path;

    /// <summary>
    /// Need remove?
    /// </summary>
    public readonly bool Remove;

    /// <summary>
    /// Create new instance of temporary folder that will be deleted after dispose
    /// </summary>
    /// <param name="path">Temporary folder</param>
    /// <param name="remove">Need remove?</param>
    public TempFolder(string path, bool remove) {
        Path = path;
        Remove = remove;
    }
        
    public void Dispose() {
        if (Remove && Directory.Exists(Path)) {
            Directory.Delete(Path, true);
        }
    }
}