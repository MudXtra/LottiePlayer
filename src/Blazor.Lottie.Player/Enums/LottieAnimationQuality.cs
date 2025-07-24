using System.ComponentModel;

namespace Blazor.Lottie.Player;

/// <summary>
/// Specifies the rendering quality level for Lottie animations.
/// </summary>
/// <remarks>
/// Quality affects the precision of curve calculations and overall visual fidelity.
/// Higher quality settings provide smoother curves and better visual accuracy but 
/// require more computational resources.
/// </remarks>
public enum LottieAnimationQuality
{
    /// <summary>
    /// Low quality rendering with basic curve precision.
    /// </summary>
    /// <remarks>
    /// Fastest performance with reduced visual fidelity. Best for simple animations
    /// or when performance is critical (mobile devices, multiple simultaneous animations).
    /// </remarks>
    [Description("low")]
    Low,

    /// <summary>
    /// Medium quality rendering with balanced precision and performance.
    /// </summary>
    /// <remarks>
    /// Good balance between visual quality and performance. Suitable for most
    /// general-purpose animations and typical web applications.
    /// </remarks>
    [Description("medium")]
    Medium,

    /// <summary>
    /// High quality rendering with maximum curve precision and visual fidelity.
    /// </summary>
    /// <remarks>
    /// Best visual quality with smooth, precise curves. Use for detailed animations
    /// where visual perfection is important. May impact performance on lower-end devices.
    /// </remarks>
    [Description("high")]
    High
}
