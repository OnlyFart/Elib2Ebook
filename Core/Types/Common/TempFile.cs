using System;
using System.IO;
using System.Threading.Tasks;

namespace Core.Types.Common; 

public class TempFile : IDisposable {
    public string Name;

    public string Extension;
    
    public Uri Url { get; set; }
    
    public string Directory { get; set; }

    public byte[] Content => File.Exists(FilePath) ?  File.ReadAllBytes(FilePath) : [];

    public string FullName => $"{Name}{Extension}";
    
    public string FilePath => Path.Combine(Directory, FullName);
    
    private TempFile(Uri url, string directory, string name, string extension) {
        Directory = directory;
        Url = url;
        Name = name;
        Extension = extension;
    }
    
    public static async Task<TempFile> Create(Uri url, string directory, string fullName, byte[] content) {
        return await Create(url, directory, Path.GetFileNameWithoutExtension(fullName), Path.GetExtension(fullName), content);
    }
    
    public static async Task<TempFile> Create(Uri url, string directory, string fullName, Stream stream) {
        return await Create(url, directory, Path.GetFileNameWithoutExtension(fullName), Path.GetExtension(fullName), stream);
    }

    public static async Task<TempFile> Create(Uri url, string directory, string name, string extension, byte[] content) {
        var image = new TempFile(url, directory, name, extension);
        await File.WriteAllBytesAsync(image.FilePath, content);
        return image;
    }
    
    public static async Task<TempFile> Create(Uri url, string directory, string name, string extension, Stream stream) {
        if (stream.Length == 0) {
            return default;
        }
        
        var tempFile = new TempFile(url, directory, name, extension);

        await using var file = File.OpenWrite(tempFile.FilePath);
        await stream.CopyToAsync(file);

        return tempFile;
    }

    public Stream GetStream() {
        return File.OpenRead(FilePath);
    }

    public void Dispose() {
        if (File.Exists(FilePath)) {
            File.Delete(FilePath);
        }
    }
}