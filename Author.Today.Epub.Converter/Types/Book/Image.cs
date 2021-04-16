using System.Collections;
using EpubSharp;

namespace Author.Today.Epub.Converter.Types.Book {
    public record Image(string Path, byte[] Content) {
        public ImageFormat Format => GetImageFormat(Path);
        
        private static ImageFormat GetImageFormat(string path) {
            if (path.EndsWith(".jpg")) {
                return ImageFormat.Jpeg;
            }

            if (path.EndsWith(".gif")) {
                return ImageFormat.Gif;
            }

            if (path.EndsWith(".png")) {
                return ImageFormat.Png;
            }

            return path.EndsWith(".svg") ? ImageFormat.Svg : ImageFormat.Jpeg;
        }
    }
}
