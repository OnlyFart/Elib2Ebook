using System;
using EpubSharp;

namespace Elib2Ebook.Types.Book; 

public record Image(byte[] Content) {
    public ImageFormat Format => GetImageFormat(Path);

    private string _path;

    public string Path {
        get => _path;
        set {
            if (string.IsNullOrWhiteSpace(value)) {
                _path = Guid.NewGuid() + ".jpg";
            } else {
                _path = Guid.NewGuid() + "." + GetExtension(value);
            }
        }
    }
    
    private static ImageFormat GetImageFormat(string path) {
        if (path.EndsWith(".jpg", StringComparison.InvariantCultureIgnoreCase)) {
            return ImageFormat.Jpeg;
        }

        if (path.EndsWith(".gif", StringComparison.InvariantCultureIgnoreCase)) {
            return ImageFormat.Gif;
        }

        if (path.EndsWith(".png", StringComparison.InvariantCultureIgnoreCase)) {
            return ImageFormat.Png;
        }

        return path.EndsWith(".svg", StringComparison.InvariantCultureIgnoreCase) ? ImageFormat.Svg : ImageFormat.Jpeg;
    }

    private static string GetExtension(string path) {
        return GetImageFormat(path) switch {
            ImageFormat.Gif => "jpg",
            ImageFormat.Png => "png",
            ImageFormat.Jpeg => "jpg",
            ImageFormat.Svg => "jpg",
            _ => "jpg"
        };
    }

    public string Extension => GetExtension(Path);
}