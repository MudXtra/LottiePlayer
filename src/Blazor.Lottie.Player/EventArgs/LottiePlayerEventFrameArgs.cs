namespace Blazor.Lottie.Player;
/// <summary>
/// Event arguments for the Lottie player loaded event.
/// </summary>
public record LottiePlayerEventFrameArgs
{
    /// <summary>
    /// Gets the ID of the event.
    /// </summary>
    public string Type { get; init; } = "enterFrame";

    /// <summary>
    /// Gets the current frame of the animation as a zero-based index.
    /// </summary>
    public double CurrentTime { get; init; }

    /// <summary>
    /// Gets the total number of frames to be processed.
    /// </summary>
    public double TotalTime { get; init; }

    /// <summary>
    /// The current direction of the Lottie animation playback.
    /// </summary>
    public LottieAnimationDirection Direction { get; init; } = LottieAnimationDirection.Forward;

}
