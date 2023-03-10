using Photoshop.Domain.Scaling;

namespace Photoshop.View;

public record ScalingData(float B, float C, ScalingType ScalingType, int Width, int Height);