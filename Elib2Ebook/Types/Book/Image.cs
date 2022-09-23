using System;
using System.IO;
using System.Threading.Tasks;

namespace Elib2Ebook.Types.Book; 

public class Image {
    private readonly string _name;
    
    public Uri Url { get; set; }
    
    public string Directory { get; set; }

    public byte[] Content => GetContent().Result;
    
    public string Extension => GetExtension(Name);
    
    public string FilePath => Path.Combine(Directory, Name);

    private Image(Uri url, string directory, string name) {
        Directory = directory;
        Url = url;
        Name = name;
    }

    public static async Task<Image> Create(Uri url, string directory, string name, byte[] content) {
        var image = new Image(url, directory, name);
        await File.WriteAllBytesAsync(image.FilePath, content);
        return image;
    }
    
    public static async Task<Image> Create(Uri url, string directory, string name, Stream stream) {
        if (stream.Length == 0) {
            return default;
        }
        
        var image = new Image(url, directory, name);

        await using var file = File.OpenWrite(image.FilePath);
        await stream.CopyToAsync(file);

        return image;
    }
    
    public Task<byte[]> GetContent() {
        return File.ReadAllBytesAsync(FilePath);
    }
    
    public Stream GetStream() {
        return File.OpenRead(FilePath);
    }

    public string Name {
        get => _name;
        private init => _name = string.IsNullOrWhiteSpace(value) ? Guid.NewGuid() + ".jpg" : Guid.NewGuid() + "." + GetExtension(value);
    }

    private static string GetExtension(string name) {
        foreach (var ext in new[] { "jpg", "png", "jpg", "svg" }) {
            if (name.EndsWith(ext)) {
                return ext;
            }
        }

        return "jpg";
    }
}