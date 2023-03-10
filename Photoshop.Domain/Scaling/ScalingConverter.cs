namespace Photoshop.Domain.Scaling;

public class ScalingConverter : IScalingConverter
{
    private float b = 0.5f, c = 0.5f;

    private const int LanchozA = 3;

    public void SetB(float value)
    {
        b = value;
    }

    public void SetC(float value)
    {
        c = value;
    }

    private void ScaleArrayByNearestNeighbour(Span<float> array, Span<float> newArray)
    {
        float oldSize = array.Length / 3, newSize = newArray.Length / 3;

        for (int i = 0; i < newSize; i++)
        {
            int oldIndex = (int)Math.Round(i * (oldSize - 1) / (newSize - 1));
            newArray[i * 3] = array[oldIndex * 3];
            newArray[i * 3 + 1] = array[oldIndex * 3 + 1];
            newArray[i * 3 + 2] = array[oldIndex * 3 + 2];
        }
    }

    private void ScaleArrayByBilinear(Span<float> array, Span<float> newArray)
    {
        float oldSize = array.Length / 3, newSize = newArray.Length / 3;

        for (int i = 0; i < newSize; i++)
        {
            float oldPosition = i * (oldSize - 1) / (newSize - 1);
            float floorCoef = oldPosition - (float)Math.Floor(oldPosition);
            float ceilCoef = (float)Math.Ceiling(oldPosition) - oldPosition;

            int oldIndex = (int)oldPosition;
            if (floorCoef == 0)
            {
                newArray[i * 3] = array[oldIndex * 3];
                newArray[i * 3 + 1] = array[oldIndex * 3 + 1];
                newArray[i * 3 + 2] = array[oldIndex * 3 + 2];
                continue;
            }


            newArray[i * 3] = array[oldIndex * 3] * floorCoef + array[(oldIndex + 1) * 3] * ceilCoef;
            newArray[i * 3 + 1] = array[oldIndex * 3 + 1] * floorCoef + array[(oldIndex + 1) * 3 + 1] * ceilCoef;
            newArray[i * 3 + 2] = array[oldIndex * 3 + 2] * floorCoef + array[(oldIndex + 1) * 3 + 2] * ceilCoef;
        }
    }

    private void ScaleArrayByLanczos3(Span<float> array, Span<float> newArray)
    {
        float oldSize = array.Length / 3, newSize = newArray.Length / 3;

        for (int i = 0; i < newSize; i++)
        {
            float oldPosition = (i * (oldSize - 1) / (newSize - 1));
            int oldIndex = (int)oldPosition;

            for (int j = Math.Max(0, -LanchozA + oldIndex + 1); j < Math.Min(oldSize, LanchozA + oldIndex); j++)
            {
                float coef = 1;
                float exprX = oldPosition - j;

                if (exprX != 0)
                {
                    coef = (float)(LanchozA * Math.Sin(Math.PI * exprX) * Math.Sin(Math.PI * exprX / LanchozA) /
                                   Math.PI / Math.PI / exprX / exprX);
                }

                for (int channel = 0; channel < 3; channel++)
                {
                    newArray[i * 3 + channel] += array[j * 3 + channel] * coef;
                }
            }

            for (int channel = 0; channel < 3; channel++)
            {
                newArray[i * 3 + channel] = Math.Max(0, Math.Min(255, newArray[i * 3 + channel]));
            }
        }
    }

    private void ScaleArrayByBcSplines(Span<float> array, Span<float> newArray)
    {
        float oldSize = array.Length / 3, newSize = newArray.Length / 3;

        for (int i = 0; i < newSize; i++)
        {
            float oldPosition = i * (oldSize - 1) / (newSize - 1);
            float d = oldPosition - (float)Math.Floor(oldPosition);

            int oldIndex = (int)oldPosition;

            for (int col = 0; col < 3; col++)
            {
                float p0, p1, p2, p3;
                p1 = array[oldIndex * 3 + col];

                if (oldIndex > 0)
                {
                    p0 = array[(oldIndex - 1) * 3 + col];
                }
                else
                {
                    p0 = p1;
                }

                if (oldIndex < oldSize - 1)
                {
                    p2 = array[(oldIndex + 1) * 3 + col];
                }
                else
                {
                    p2 = p1;
                }

                if (oldIndex < oldSize - 2)
                {
                    p3 = array[(oldIndex + 2) * 3 + col];
                }
                else
                {
                    p3 = p2;
                }

                newArray[i * 3 + col] = d * d * d * ((-1f / 6 * b - c) * p0 + (-3f / 2 * b - c + 2) * p1 +
                                                     (3f / 2 * b + c - 2) * p2 + (1f / 6 * b + c) * p3) +
                                        d * d * ((1f / 2 * b + 2 * c) * p0 + (2 * b + c - 3) * p1 +
                                            (-5f / 2 * b - 2 * c + 3) * p2 - c * p3) +
                                        d * ((-1f / 2 * b - c) * p0 + (1f / 2 * b + c) * p2)
                                        + 1f / 6 * b * p0 + (-1f / 3 * b + 1) * p1 + 1f / 6 * b * p2;

                newArray[i * 3 + col] = Math.Min(255f, Math.Max(0, newArray[i * 3 + col]));
            }
        }
    }

    private void ScaleArray(Span<float> array, Span<float> newArray, ScalingType scalingType)
    {
        switch (scalingType)
        {
            case ScalingType.NearestNeighbour:
                ScaleArrayByNearestNeighbour(array, newArray);
                break;
            case ScalingType.Bilinear:
                ScaleArrayByBilinear(array, newArray);
                break;
            case ScalingType.Lanczos3:
                ScaleArrayByLanczos3(array, newArray);
                break;
            case ScalingType.BcSplines:
                ScaleArrayByBcSplines(array, newArray);
                break;
        }
    }

    private ImageData ScaleWidth(ImageData imageData, int newWidth, ScalingType scalingType)
    {
        int height = imageData.Height;
        int width = imageData.Width;
        float[] newPixels = new float[height * newWidth * 3];

        for (int i = 0; i < height; i++)
        {
            ScaleArray(new Span<float>(imageData.Pixels, i * width * 3, width * 3),
                new Span<float>(newPixels, i * newWidth * 3, newWidth * 3), scalingType);
        }

        return new ImageData(newPixels, PixelFormat.Rgb, height, newWidth, imageData.Gamma);
    }

    private ImageData ScaleHeight(ImageData imageData, int newHeight, ScalingType scalingType)
    {
        int height = imageData.Height;
        int width = imageData.Width;
        float[] newPixels = new float[newHeight * width * 3];
        float[] bufferOldHeight = new float[height * 3];
        float[] bufferNewHeight = new float[newHeight * 3];

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                bufferOldHeight[j * 3] = imageData.Pixels[j * width * 3 + i * 3];
                bufferOldHeight[j * 3 + 1] = imageData.Pixels[j * width * 3 + i * 3 + 1];
                bufferOldHeight[j * 3 + 2] = imageData.Pixels[j * width * 3 + i * 3 + 2];
            }

            for (int j = 0; j < newHeight * 3; j++)
            {
                bufferNewHeight[j] = 0;
            }

            ScaleArray(bufferOldHeight, bufferNewHeight, scalingType);

            for (int j = 0; j < newHeight; j++)
            {
                newPixels[j * width * 3 + i * 3] = bufferNewHeight[j * 3];
                newPixels[j * width * 3 + i * 3 + 1] = bufferNewHeight[j * 3 + 1];
                newPixels[j * width * 3 + i * 3 + 2] = bufferNewHeight[j * 3 + 2];
            }
        }

        return new ImageData(newPixels, PixelFormat.Rgb, newHeight, width, imageData.Gamma);
    }

    // Альтернативная реализация билинейной интерполяции, работает, но не используется
    private void Scale2DArrayByBilinear(Span<float> array, Span<float> newArray, int width, int height, int newWidth,
        int newHeight)
    {
        for (int i = 0; i < newHeight; i++)
        {
            float oldPositionY = i * (height - 1.0f) / (newHeight);
            float floorCoefY = oldPositionY - (float)Math.Floor(oldPositionY);
            float ceilCoefY = (float)Math.Ceiling(oldPositionY) - oldPositionY;
            int oldIndexY = (int)oldPositionY;

            for (int j = 0; j < newWidth; j++)
            {
                float oldPositionX = j * (width - 1.0f) / (newWidth);
                float floorCoefX = oldPositionX - (float)Math.Floor(oldPositionX);
                float ceilCoefX = (float)Math.Ceiling(oldPositionX) - oldPositionX;
                int oldIndexX = (int)oldPositionX;

                for (int col = 0; col < 3; col++)
                {
                    if (floorCoefX == 0 && floorCoefY == 0)
                    {
                        newArray[i * newWidth * 3 + j * 3 + col] = array[oldIndexY * width * 3 + oldIndexX * 3 + col];
                    }
                    else if (floorCoefX == 0)
                    {
                        newArray[i * newWidth * 3 + j * 3 + col] =
                            array[oldIndexY * width * 3 + oldIndexX * 3 + col] * floorCoefY +
                            array[(oldIndexY + 1) * width * 3 + oldIndexX * 3 + col] * ceilCoefY;
                    }
                    else if (floorCoefY == 0)
                    {
                        newArray[i * newWidth * 3 + j * 3 + col] =
                            array[oldIndexY * width * 3 + oldIndexX * 3 + col] * floorCoefX +
                            array[oldIndexY * width * 3 + (oldIndexX + 1) * 3 + col] * ceilCoefX;
                    }
                    else
                    {
                        newArray[i * newWidth * 3 + j * 3 + col] =
                            (array[oldIndexY * width * 3 + oldIndexX * 3 + col] * floorCoefX +
                             array[oldIndexY * width * 3 + (oldIndexX + 1) * 3 + col] * ceilCoefX) * floorCoefY +
                            (array[(oldIndexY + 1) * width * 3 + oldIndexX * 3 + col] * floorCoefX +
                             array[(oldIndexY + 1) * width * 3 + (oldIndexX + 1) * 3 + col] * ceilCoefX) * ceilCoefY;
                    }
                }
            }
        }
    }

    // Не используется, но могут попросить использовать
    private ImageData Scale2D(ImageData imageData, ScalingType scalingType, int newWidth, int newHeight)
    {
        int width = imageData.Width;
        int height = imageData.Height;
        float[] newPixels = new float[newWidth * newHeight * 3];

        switch (scalingType)
        {
            case ScalingType.Bilinear:
                Scale2DArrayByBilinear(imageData.Pixels, newPixels, width, height, newWidth, newHeight);
                break;
        }

        return new ImageData(newPixels, PixelFormat.Rgb, newHeight, newWidth, imageData.Gamma);
    }

    public ImageData Convert(ImageData source, ScalingType scalingType, int newWidth, int newHeight)
    {
        var tempImage = source.SetPixelFormat(PixelFormat.Rgb);

        tempImage = ScaleHeight(ScaleWidth(tempImage, newWidth, scalingType), newHeight, scalingType);

        return tempImage.SetPixelFormat(source.PixelFormat);
    }
}