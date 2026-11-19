namespace Blazor.Lottie.Player;

/// <summary>
/// Represents the advanced configuration options for rendering Lottie animations.
/// </summary>
public class LottieAnimationOptions
{
    /// <summary>
    /// Gets or sets the URL of the Lottie JavaScript library used for rendering Lottie animations.
    /// </summary>
    public string LottieSourceJs { get; set; } = DefaultLottieSourceJs;

    /// <summary>
    /// The default CDN URL of the Lottie JavaScript library.
    /// </summary>
    public static readonly string DefaultLottieSourceJs = "https://cdnjs.cloudflare.com/ajax/libs/lottie-web/5.13.0/lottie.min.js";

    /// <summary>
    /// The Subresource Integrity hash of the file at <see cref="DefaultLottieSourceJs"/>.
    /// </summary>
    public static readonly string DefaultLottieSourceJsIntegrity = "sha512-uOtp2vx2X/5+tLBEf5UoQyqwAkFZJBM5XwGa7BfXDnWR+wdpRvlSVzaIVcRe3tGNsStu6UMDCeXKEnr4IBT8gA==";

    private string? _lottieSourceJsIntegrity;

    /// <summary>
    /// Gets or sets the Subresource Integrity value (for example <c>sha512-...</c>) the browser checks the
    /// <see cref="LottieSourceJs"/> script against.
    /// </summary>
    /// <remarks>
    /// <para>When not set, the hash of the default CDN file is used only while <see cref="LottieSourceJs"/> is
    /// still the default URL; a different <see cref="LottieSourceJs"/> is loaded without integrity checking.</para>
    /// <para>Set to an empty string to turn integrity checking off. Set to <c>null</c> to restore the automatic default
    /// described above. If the check fails, the local copy is used.</para>
    /// <para>A non-empty integrity makes the loader set <c>crossorigin="anonymous"</c> on the script, so a custom
    /// <see cref="LottieSourceJs"/> host must send CORS headers (<c>Access-Control-Allow-Origin</c>); otherwise the
    /// browser blocks the script and the bundled local copy is used. Same-origin URLs are unaffected.</para>
    /// </remarks>
    public string? LottieSourceJsIntegrity
    {
        get => _lottieSourceJsIntegrity
            ?? (string.Equals(LottieSourceJs, DefaultLottieSourceJs, StringComparison.Ordinal) ? DefaultLottieSourceJsIntegrity : null);
        set => _lottieSourceJsIntegrity = value;
    }

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
    /// <remarks>Two instances are considered equal if they have the same <see cref="LottieSourceJs"/>, <see cref="LottieSourceJsIntegrity"/>, <see
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
            && string.Equals(LottieSourceJsIntegrity, other.LottieSourceJsIntegrity, StringComparison.Ordinal)
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
            LottieSourceJsIntegrity != null ? StringComparer.Ordinal.GetHashCode(LottieSourceJsIntegrity) : 0,
            AnimationQuality,
            SmoothFrameInterpolation
        );
    }
}
