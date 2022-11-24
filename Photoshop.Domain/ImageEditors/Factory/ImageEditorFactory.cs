﻿namespace Photoshop.Domain.ImageEditors.Factory;

public class ImageEditorFactory : IImageEditorFactory
{
    private readonly IColorSpaceConverter _colorSpaceConverter;
    private readonly IGammaConverter _gammaConverter;

    public ImageEditorFactory(IColorSpaceConverter colorSpaceConverter, IGammaConverter gammaConverter)
    {
        _colorSpaceConverter = colorSpaceConverter;
        _gammaConverter = gammaConverter;
    }

    public IImageEditor GetImageEditor(ImageData imageData, ColorSpace colorSpace, float imageGamma, float outputGamma)
    {
        return imageData.PixelFormat switch
        {
            PixelFormat.Rgb => new RgbImageEditor(imageData, colorSpace, imageGamma, outputGamma, _colorSpaceConverter, _gammaConverter),
            PixelFormat.Gray => new GrayImageEditor(imageData, imageGamma, outputGamma, _gammaConverter),
            _ => throw new NotSupportedException($"Данный формат картинки не поддерживается: {imageData.PixelFormat}")
        };
    }
}