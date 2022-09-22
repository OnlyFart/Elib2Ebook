using System;
using System.IO;
using System.Threading.Tasks;

namespace Elib2Ebook.Types.Book; 

public class Image {
    private readonly string _name;
    public string FilePath => Path.Combine(Directory, Name);
    
    public string Directory;

    public Uri Url;

    public Image(Uri url, string directory, string name, byte[] content) {
        Directory = directory;
        Url = url;
        Name = name;
        File.WriteAllBytes(FilePath, content);
    }

    public Task<byte[]> GetContent() {
        return File.ReadAllBytesAsync(FilePath);
    }

    public string Name {
        get => _name;
        private init {
            if (string.IsNullOrWhiteSpace(value)) {
                _name = Guid.NewGuid() + ".jpg";
            } else {
                _name = Guid.NewGuid() + "." + GetExtension(value);
            }
        }
    }

    private static string GetExtension(string name) {
        foreach (var ext in new[] { "jpg", "png", "jpg", "svg" }) {
            if (name.EndsWith(ext)) {
                return ext;
            }
        }

        return "jpg";
    }

    public string Extension => GetExtension(Name);
}