namespace Blazor.Lottie.Player;

/// <summary>
/// Represents the advanced configuration options for rendering Lottie animations.
/// </summary>
public class LottieAnimationOptions
{
    /// <summary>
    /// Gets or sets the URL of the Lottie JavaScript library used for rendering Lottie animations.
    /// </summary>
    public string LottieSourceJs { get; set; } = "./_content/Blazor.Lottie.Player/lottie.min.js";

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

    /// <summary>
    /// Determines whether the specified object is equal to the current instance.
    /// </summary>
    /// <remarks>Two instances are considered equal if they have the same <see cref="LottieSourceJs"/>,  <see
    /// cref="AnimationQuality"/>, and <see cref="SmoothFrameInterpolation"/> values.</remarks>
    /// <param name="obj">The object to compare with the current instance.</param>
    /// <returns><see langword="true"/> if the specified object is equal to the current instance;  otherwise, <see
    /// langword="false"/>.</returns>
    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(this, obj))
            return true;
        if (obj is not LottieAnimationOptions other)
            return false;

        return string.Equals(LottieSourceJs, other.LottieSourceJs, StringComparison.Ordinal)
            && AnimationQuality == other.AnimationQuality
            && SmoothFrameInterpolation == other.SmoothFrameInterpolation;
    }

    /// <summary>
    /// Returns a hash code for the current object.
    /// </summary>
    /// <remarks>The hash code is computed based on the values of the <see cref="LottieSourceJs"/>,  <see
    /// cref="AnimationQuality"/>, and <see cref="SmoothFrameInterpolation"/> properties.  This ensures that objects
    /// with the same property values produce the same hash code.</remarks>
    /// <returns>An integer hash code that represents the current object.</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(
            LottieSourceJs != null ? StringComparer.Ordinal.GetHashCode(LottieSourceJs) : 0,
            AnimationQuality,
            SmoothFrameInterpolation
        );
    }
}
