using Blazor.Lottie.Player.Serialization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Blazor.Lottie.Player;
/// <summary>
/// A Blazor component for rendering and controlling Lottie animations using BodyMovin.
/// </summary>
/// <remarks>The <see cref="LottiePlayer"/> component provides a flexible way to integrate Lottie animations into
/// Blazor applications. It supports various customization options, including animation source, playback settings, and
/// event callbacks for animation lifecycle events.</remarks>
public partial class LottiePlayer : ComponentBase
{
    #region Private Variables

    private ElementReference ElementRef { get; set; } = default!;
    private readonly string ElementId = Identifier.Create("lottie-");
    private IJSObjectReference? _jsModule;
    private LottiePlayerModule? _lottiePlayerModule;

    protected string Classname => $"{Class} blazor-lottie-player";

    private string? Stylename => string.IsNullOrWhiteSpace(Style) ? null : Style;

    #endregion

    #region Injected Services

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    #endregion

    #region Parameters

    /// <summary>
    /// The CSS class name(s) to apply to the container.
    /// </summary>
    [Parameter]
    public string? Class { get; set; }

    /// <summary>
    /// The inline CSS styles to apply to the component.
    /// </summary>
    [Parameter]
    public string? Style { get; set; }

    /// <summary>
    /// Sets a collection of user-defined HTML attributes represented as key-value pairs.
    /// </summary>
    /// <remarks>
    /// This property is typically used to provide additional HTML attributes during
    /// rendering such as ARIA accessibility tags, data attributes, or any other custom
    /// HTML attributes that you want to apply to the component.
    /// </remarks>
    [Parameter]
    public Dictionary<string, object>? UserAttributes { get; set; }

    /// <summary>
    /// Gets or sets the source or relative URL for the component's content.
    /// </summary>
    /// <remarks>
    /// *Required* e.g.<br/>
    /// Src="<c>https://mudxtra.github.io/LottiePlayer/newAnimation.json</c>"<br/>
    /// Src="<c>./wwwroot/lottie/newAnimation.json</c>"
    /// </remarks>
    [Parameter, EditorRequired]
    public string Src { get; set; } = default!;

    /// <summary>
    /// A value indicating whether the media should automatically start playing when loaded. If set to
    /// <c>false</c>, the media will not start playing until AnimationPaused is set to false.
    /// </summary>
    [Parameter]
    public bool AutoPlay { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the animation should loop.
    /// </summary>
    [Parameter]
    public bool Loop { get; set; } = true;

    /// <summary>
    /// Gets or sets the number of times a loop should occur. Requires <see cref="Loop"/> to be true.
    /// </summary>
    /// <remarks>
    /// Default is 0 (infinite loop).
    /// </remarks>
    [Parameter]
    public int LoopCount { get; set; } = 0;

    /// <summary>
    /// Gets or sets the type of animation to be used for rendering the Lottie animation.
    /// </summary>
    /// <remarks>
    /// Defaults to <see cref="LottieAnimationType.Svg"/>.
    /// </remarks>
    [Parameter]
    public LottieAnimationType AnimationType { get; set; } = LottieAnimationType.Svg;

    /// <summary>
    /// Gets or sets the direction in which the Lottie animation plays.
    /// </summary>
    /// <remarks>
    /// Defaults to <see cref="LottieAnimationDirection.Forward"/>.
    /// </remarks>
    [Parameter]
    public LottieAnimationDirection AnimationDirection { get; set; } = LottieAnimationDirection.Forward;

    /// <summary>
    /// Gets or sets the speed of the animation.
    /// </summary>
    [Parameter]
    public double AnimationSpeed { get; set; } = 1.0;

    /// <summary>
    /// Called when the current frame changes, with custom filtering logic
    /// </summary>
    /// <remarks>
    /// The function receives the current frame number and should return true if the event should be raised.
    /// This allows for custom throttling logic (e.g., only every 10th frame, only on specific frames, etc.)
    /// <br/>Warning: Frame updates could happen as frequently as 30-60 times per second.
    /// <br/>Defaults to <c>null</c>. Set to <c>null</c> <see cref="CurrentFrameChanged"/> will not be raised.
    /// <br/>Example: <c>frame => frame % 10 == 0</c> will raise the event every 10th frame.
    /// </remarks>
    [Parameter] public Func<double, bool>? CurrentFrameChangeFunc { get; set; }

    /// <summary>
    /// Called when the current frame changes (only when <see cref="CurrentFrameChangeFunc"/> returns true)
    /// </summary>
    [Parameter] public EventCallback<double> CurrentFrameChanged { get; set; }

    /// <summary>
    /// Called when the animation finishes loading and the elements have been added to the DOM
    /// </summary>
    [Parameter]
    public EventCallback<LottiePlayerLoadedEventArgs> AnimationLoaded { get; set; }

    /// <summary>
    /// Called when the animation completes.
    /// </summary>
    [Parameter]
    public EventCallback AnimationCompleted { get; set; }

    /// <summary>
    /// Called when an animation loop completes.
    /// </summary>
    [Parameter]
    public EventCallback AnimationLoopCompleted { get; set; }

    /// <summary>
    /// Additional Less Used Animation Options.
    /// </summary>
    /// <remarks>Refer to <see cref="LottieAnimationOptions"/> for defaults.</remarks>
    [Parameter]
    public LottieAnimationOptions AnimationOptions { get; set; } = new();

    #endregion

    #region Component Public Properties

    /// <summary>
    /// Gets the current animation frame as a double-precision value.
    /// </summary>
    public double CurrentAnimationFrame { get; private set; } = 0.0;

    /// <summary>
    /// Gets a value indicating whether the animation has been loaded and is ready for playback.
    /// </summary>
    public bool IsAnimationLoaded { get; private set; } = false;

    /// <summary>
    /// Gets a value indicating whether the animation is paused.
    /// </summary>
    public bool IsAnimationPaused { get; private set; }

    #endregion

    #region Public Methods

    /// <summary>
    /// If the animation is not already playing, this method will start playing the animation.
    /// </summary>
    /// <remarks>Should not be called before OnAfterRenderAsync.</remarks>
    /// <returns><see langword="true"/> if the animation was successfully started; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task<bool> PlayAnimationAsync()
    {
        if (JSRuntime is null)
        {
            throw new InvalidOperationException("JSRuntime is not initialized. Lottie Player failed.");
        }
        var result = await JSRuntime.InvokeAsync<bool>("BlazorLottiePlayer.playAnimation", Src, AnimationType, AnimationDirection, AnimationSpeed);
        AnimationPaused = !result;
        return result;
    }

    /// <summary>
    /// Pauses the currently playing animation in the Lottie player.
    /// </summary>
    /// <remarks>Should not be called before OnAfterRenderAsync.</remarks>
    /// <returns><see langword="true"/> if the animation was successfully paused; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task<bool> PauseAnimationAsync()
    {
        if (JSRuntime is null)
        {
            throw new InvalidOperationException("JSRuntime is not initialized. Lottie Player failed.");
        }
        var result = await JSRuntime.InvokeAsync<bool>("BlazorLottiePlayer.pauseAnimation", Src);
        AnimationPaused = result;
        return result;
    }

    #endregion

    #region Compoinent Lifecycle

    /// <summary>
    /// OnAfterRenderAsync override
    /// </summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            var initModule = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./content/Blazor.Lottie.Player/loadLottieWeb.js");
            var result = await initModule.InvokeAsync<bool>("initialize", AnimationOptions.LottieSourceJs);
            if (!result)
            {
                throw new InvalidOperationException("Failed to initialize Lottie Web Player Module");
            }
            _lottiePlayerModule = new LottiePlayerModule(JSRuntime, ElementRef);
            var lottiePlaybackOptions = new LottiePlaybackOptions
            {
                AutoPlay = AutoPlay,
                Renderer = AnimationType.ToDescription(),
                Loop = Loop ? (object)(LoopCount > 0 ? LoopCount : true) : false,
                Direction = (int)AnimationDirection,
                Speed = AnimationSpeed,
                SetSubFrame = AnimationOptions.SmoothFrameInterpolation,
                Quality = AnimationOptions.AnimationQuality.ToDescription()
            };
            result = await _lottiePlayerModule.InitializeAsync(lottiePlaybackOptions);
            if (!result)
            {
                throw new InvalidOperationException("Failed to initialize Lottie Animation");
            }
        }
    }

    #endregion
}

