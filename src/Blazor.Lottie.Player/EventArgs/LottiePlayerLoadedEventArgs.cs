namespace Blazor.Lottie.Player;
/// <summary>
/// Event arguments for the Lottie player loaded event.
/// </summary>
public record LottiePlayerLoadedEventArgs
{
    /// <summary>
    /// Gets the ID of the Lottie player element.
    /// </summary>
    public string ElementId { get; init; } = default!;

    /// <summary>
    /// Gets the current frame of the animation as a zero-based index.
    /// </summary>
    public double CurrentFrame { get; init; }

    /// <summary>
    /// Gets the total number of frames processed.
    /// </summary>
    public double TotalFrames { get; init; }

}
