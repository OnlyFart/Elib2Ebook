using Core.Extensions;
using EpubSharp;
using EpubSharp.Format;

namespace Core.Tests.Extensions;

public class ImageFormatExtensionsTests
{
    [Theory]
    [InlineData(ImageFormat.Gif, EpubContentType.ImageGif)]
    [InlineData(ImageFormat.Png, EpubContentType.ImagePng)]
    [InlineData(ImageFormat.Jpeg, EpubContentType.ImageJpeg)]
    [InlineData(ImageFormat.Svg, EpubContentType.ImageSvg)]
    public void ToEpubContentType(ImageFormat fmt, EpubContentType expected)
    {
        Assert.Equal(expected, fmt.ToEpubContentType());
    }

    [Fact]
    public void ToEpubContentType_Unknown()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ((ImageFormat)999).ToEpubContentType());
    }
}
