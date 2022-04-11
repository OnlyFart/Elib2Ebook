using System;
using EpubSharp;
using EpubSharp.Format;

namespace Elib2Ebook.Extensions; 

public static class ImageFormatExtensions {
    public static EpubContentType ToEpubContentType(this ImageFormat self) {
        return self switch {
            ImageFormat.Gif => EpubContentType.ImageGif,
            ImageFormat.Png => EpubContentType.ImagePng,
            ImageFormat.Jpeg => EpubContentType.ImageJpeg,
            ImageFormat.Svg => EpubContentType.ImageSvg,
            _ => throw new ArgumentOutOfRangeException(nameof(self), self, null)
        };
    }
}