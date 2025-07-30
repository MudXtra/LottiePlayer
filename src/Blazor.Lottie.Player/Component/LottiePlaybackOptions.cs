
namespace Blazor.Lottie.Player;
/// <summary>
/// Configuration options for Lottie animation playback and rendering.
/// </summary>
public class LottiePlaybackOptions
{
    /// <summary>
    /// Gets or sets whether the animation should start playing automatically.
    /// </summary>
    /// <remarks>Default is true.</remarks>
    public bool AutoPlay { get; set; } = true;

    /// <summary>
    /// Gets or sets the type of Lottie animation to render.
    /// </summary>
    /// <remarks>Default is SVG for best feature support.</remarks>
    public string Renderer { get; set; } = LottieAnimationType.Svg.ToDescription();

    /// <summary>
    /// Gets or sets the loop behavior for the animation.
    /// </summary>
    /// <remarks>
    /// Can be true (infinite loop), false (no loop), or a positive integer 
    /// for a specific number of loops.
    /// </remarks>
    public object Loop { get; set; } = true;

    /// <summary>
    /// Gets or sets the playback direction.
    /// </summary>
    /// <remarks>1 for forward, -1 for reverse. Default is 1.</remarks>
    public int Direction { get; set; } = (int)LottieAnimationDirection.Forward;

    /// <summary>
    /// Gets or sets the playback speed multiplier.
    /// </summary>
    /// <remarks>1.0 is normal speed, 2.0 is double speed, 0.5 is half speed. Default is 1.0.</remarks>
    public double Speed { get; set; } = 1.0;

    /// <summary>
    /// Gets or sets whether to enable smooth frame interpolation for high refresh rate displays.
    /// </summary>
    /// <remarks>Default is true for smooth animations.</remarks>
    public bool SetSubFrame { get; set; } = true;

    /// <summary>
    /// Gets or sets the rendering quality level.
    /// </summary>
    /// <remarks>Default is High.</remarks>
    public string Quality { get; set; } = LottieAnimationQuality.High.ToDescription();

    /// <summary>
    /// Whether to enable the enterFrame event.
    /// </summary>
    /// <remarks>
    /// If <c>CurrentFrameChangedFunc</c> is null this should be false.
    /// </remarks>
    public bool EnterFrameEvent { get; set; } = false;
}
