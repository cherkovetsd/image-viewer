﻿namespace Photoshop.Domain.ImageEditors.Factory;

public class ImageEditorFactory : IImageEditorFactory
{
    private readonly IColorSpaceConverter _colorSpaceConverter;
    private readonly IGammaConverter _gammaConverter;
    private readonly IDitheringConverter _ditheringConverter;

    public ImageEditorFactory(IColorSpaceConverter colorSpaceConverter, IGammaConverter gammaConverter, IDitheringConverter ditheringConverter)
    {
        _colorSpaceConverter = colorSpaceConverter;
        _gammaConverter = gammaConverter;
        _ditheringConverter = ditheringConverter;
    }

    public IImageEditor GetImageEditor(ImageData imageData, ColorSpace colorSpace, float imageGamma)
    {
        return imageData.PixelFormat switch
        {
            PixelFormat.Rgb => new RgbImageEditor(imageData, colorSpace, imageGamma, _colorSpaceConverter, _gammaConverter, _ditheringConverter),
            PixelFormat.Gray => new GrayImageEditor(imageData, imageGamma, _gammaConverter, _ditheringConverter),
            _ => throw new NotSupportedException($"Данный формат картинки не поддерживается: {imageData.PixelFormat}")
        };
    }
}