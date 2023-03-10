using Photoshop.Domain.Scaling;

namespace Photoshop.Domain.ImageEditors;

public interface IImageEditor
{
    ImageData GetData();

    ImageData GetDitheredData(DitheringType ditheringType, int ditheringDepth);

    ImageData GetRgbData(float gamma, DitheringType ditheringType, int ditheringDepth, bool[]? channels);

    void SetColorSpace(ColorSpace newColorSpace);

    void SetGamma(float gamma);

    void ConvertGamma(float gamma);

    void Scale(float b, float c, ScalingType scalingType, int width, int height);
}