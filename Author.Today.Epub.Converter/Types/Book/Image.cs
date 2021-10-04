using System;
using EpubSharp;

namespace Author.Today.Epub.Converter.Types.Book {
    public record Image(string Path, byte[] Content) {
        public ImageFormat Format => GetImageFormat(Path);
        public string Path { get; } = string.IsNullOrWhiteSpace(Path) ? Guid.NewGuid() + ".jpg" : Path;

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

        public string Extension {
            get {
                return GetImageFormat(Path) switch {
                    ImageFormat.Gif => "jpg",
                    ImageFormat.Png => "png",
                    ImageFormat.Jpeg => "jpg",
                    ImageFormat.Svg => "jpg",
                    _ => "jpg"
                };
            }
        }
    }
}
