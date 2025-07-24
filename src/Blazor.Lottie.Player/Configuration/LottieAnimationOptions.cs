namespace Blazor.Lottie.Player;

/// <summary>
/// Represents the configuration options for rendering Lottie animations.
/// </summary>
public class LottieAnimationOptions
{
    /// <summary>
    /// Gets or sets the URL of the Lottie JavaScript library used for rendering Lottie animations.
    /// </summary>
    public string LottieSourceJs { get; set; } = "https://cdnjs.cloudflare.com/ajax/libs/lottie-web/5.13.0/lottie.min.js";

    /// <summary>
    /// Gets or sets the quality level of the Lottie animation rendering. This is a global setting.
    /// </summary>
    /// <remarks>Defaults to <see cref="LottieAnimationQuality.High"/>. Higher quality levels may
    /// result in smoother animations but could increase resource usage.</remarks>
    public LottieAnimationQuality AnimationQuality { get; set; } = LottieAnimationQuality.High;

    /// <summary>
    /// Gets or sets whether to enable smooth frame interpolation for high refresh rate displays.
    /// </summary>
    /// <remarks>
    /// <para>When <c>true</c> (default), the animation will interpolate between keyframes to create 
    /// smooth motion that adapts to your display's refresh rate (60Hz, 120Hz, etc.).</para>
    /// <para>When <c>false</c>, the animation will only update on the original After Effects keyframes, 
    /// which may appear choppy on high refresh displays but offers better performance and 
    /// pixel-perfect reproduction of the original animation timing.</para>
    /// </remarks>
    public bool SmoothFrameInterpolation { get; set; } = true;
}
