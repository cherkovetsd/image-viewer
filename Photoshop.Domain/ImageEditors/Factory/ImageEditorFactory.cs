using Photoshop.Domain.Scaling;

namespace Photoshop.Domain.ImageEditors.Factory;

public class ImageEditorFactory : IImageEditorFactory
{
    private readonly IColorSpaceConverter _colorSpaceConverter;
    private readonly IGammaConverter _gammaConverter;
    private readonly IDitheringConverter _ditheringConverter;
    private readonly IScalingConverter _scalingConverter;

    public ImageEditorFactory(
        IColorSpaceConverter colorSpaceConverter,
        IGammaConverter gammaConverter,
        IDitheringConverter ditheringConverter,
        IScalingConverter scalingConverter)
    {
        _colorSpaceConverter = colorSpaceConverter;
        _gammaConverter = gammaConverter;
        _ditheringConverter = ditheringConverter;
        _scalingConverter = scalingConverter;
    }

    public IImageEditor GetImageEditor(ImageData imageData, ColorSpace colorSpace)
    {
        return imageData.PixelFormat switch
        {
            PixelFormat.Rgb => new RgbImageEditor(
                imageData,
                colorSpace,
                _colorSpaceConverter,
                _gammaConverter,
                _ditheringConverter,
                _scalingConverter),

            PixelFormat.Gray => new GrayImageEditor(
                imageData,
                _gammaConverter,
                _ditheringConverter,
                _scalingConverter),

            _ => throw new NotSupportedException($"Данный формат картинки не поддерживается: {imageData.PixelFormat}")
        };
    }
}