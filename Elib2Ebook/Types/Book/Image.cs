using System;

namespace Elib2Ebook.Types.Book; 

public record Image(byte[] Content) {
    private readonly string _name;

    public string Name {
        get => _name;
        init {
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