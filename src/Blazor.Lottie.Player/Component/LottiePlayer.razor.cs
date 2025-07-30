using Blazor.Lottie.Player.Extensions;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Blazor.Lottie.Player;
/// <summary>
/// A Blazor component for rendering and controlling Lottie animations using BodyMovin.
/// </summary>
/// <remarks>The <see cref="LottiePlayer"/> component provides a flexible way to integrate Lottie animations into
/// Blazor applications. It supports various customization options, including animation source, playback settings, and
/// event callbacks for animation lifecycle events.</remarks>
public partial class LottiePlayer : ComponentBase, IAsyncDisposable
{
    #region Private Variables

    private ElementReference ElementRef = default!;
    /// <summary>
    /// Represents the unique identifier for a Lottie element's surrounding div.
    /// </summary>
    public readonly string ElementId = Identifier.Create("lottie-");
    internal LottiePlayerModule? _lottiePlayerModule;
    private int _initCount = 0;

    /// <summary>
    /// The class list for the surrounding div, which includes the base class and the Blazor Lottie Player class.
    /// </summary>
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
    /// Called when the Lottie player is initialized and the DOM is ready.
    /// </summary>
    [Parameter]
    public EventCallback<LottiePlayerLoadedEventArgs> DOMLoaded { get; set; }

    /// <summary>
    /// Called when the animation is fully loaded and ready for playback.
    /// </summary>
    [Parameter]
    public EventCallback<LottiePlayerLoadedEventArgs> AnimationReady { get; set; }

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
    /// Gets the current animation frame as a double-precision value. This is updated during playback by the EventCallback of CurrentFrameChanged.
    /// </summary>
    public double CurrentAnimationFrame { get; private set; } = 0.0;

    /// <summary>
    /// Gets a value indicating whether the animation has been loaded and is ready for playback.
    /// </summary>
    public bool IsAnimationLoaded { get; private set; } = false;

    /// <summary>
    /// Gets a value indicating whether the animation is paused.
    /// </summary>
    public bool IsAnimationPaused { get; private set; } = false;

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
        if (_lottiePlayerModule == null)
        {
            throw new InvalidOperationException("Lottie Player Module failed.");
        }

        var result = await _lottiePlayerModule.PlayAsync();
        IsAnimationPaused = !result;
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
        if (_lottiePlayerModule == null)
        {
            throw new InvalidOperationException("Lottie Player Module failed.");
        }

        IsAnimationPaused = await _lottiePlayerModule.PauseAsync();
        return IsAnimationPaused;
    }

    /// <summary>
    /// Stops the currently playing animation asynchronously.
    /// </summary>
    /// <remarks>This method halts the animation playback if an animation is currently running.  The method
    /// returns a boolean indicating whether the operation was successful.</remarks>
    /// <returns><see langword="true"/> if the animation was successfully stopped; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the Lottie Player Module is not initialized.</exception>
    public async Task<bool> StopAnimationAsync()
    {
        if (_lottiePlayerModule == null)
        {
            throw new InvalidOperationException("Lottie Player Module failed.");
        }
        IsAnimationPaused = await _lottiePlayerModule.StopAsync();
        return IsAnimationPaused;
    }

    /// <summary>
    /// Asynchronously sets the playback speed for the Lottie animation.
    /// </summary>
    /// <param name="speed">The desired playback speed. Must be a positive value, where 1.0 represents normal speed.</param>
    /// <exception cref="InvalidOperationException">Thrown if the Lottie Player Module is not initialized.</exception>
    public async Task<bool> SetSpeedAsync(double speed)
    {
        if (_lottiePlayerModule == null)
        {
            throw new InvalidOperationException("Lottie Player Module failed.");
        }
        var result = await _lottiePlayerModule.SetSpeedAsync(speed);
        return result;
    }

    /// <summary>
    /// Sets the playback direction of the Lottie animation asynchronously.
    /// </summary>
    /// <param name="direction">The desired playback direction of the animation. Must be a valid <see cref="LottieAnimationDirection"/> value.</param>
    /// <exception cref="InvalidOperationException">Thrown if the Lottie Player Module is not initialized.</exception>
    public async Task<bool> SetDirectionAsync(LottieAnimationDirection direction)
    {
        if (_lottiePlayerModule == null)
        {
            throw new InvalidOperationException("Lottie Player Module failed.");
        }
        var result = await _lottiePlayerModule.SetDirectionAsync(direction);
        return result;
    }

    #endregion

    #region Events

    private void HandleDOMLoaded(object? sender, LottiePlayerLoadedEventArgs args)
    {
        IsAnimationLoaded = true;
        await DOMLoaded.InvokeAsync(args);
    }

    private void HandleAnimationLoaded(object? sender, LottiePlayerLoadedEventArgs args)
    {
        IsAnimationLoaded = true;
        await AnimationReady.InvokeAsync(args);
    }

    private void HandleAnimationCompleted(object? sender, bool completed)
    {
        AnimationCompleted.InvokeAsync();
    }

    private void HandleAnimationLoopCompleted(object? sender, bool completed)
    {
        AnimationLoopCompleted.InvokeAsync();
    }

    private void HandleCurrentFrameChanged(object? sender, double frame)
    {
        CurrentAnimationFrame = frame;
        if (CurrentFrameChangeFunc == null || CurrentFrameChangeFunc.Invoke(frame))
        {
            await CurrentFrameChanged.InvokeAsync(frame);
        }
    }

    #endregion

    /// <summary>
    /// Disposes the LottiePlayer Module if it exists, cleaning up resources and references. Then re-initializes it with
    /// any updated options.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    private async Task InitializeLottieModule()
    {
        // dispose the existing module if it exists
        if (_lottiePlayerModule != null)
        {
            // cancel event handlers to prevent memory leaks
            EventSubscription(false);

            await _lottiePlayerModule.DisposeAsync();
            _lottiePlayerModule = null;
            IsAnimationLoaded = false;
            IsAnimationPaused = false;
        }
        // initialize the new module with the current parameters
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
        IsAnimationLoaded = await _lottiePlayerModule.InitializeAsync(lottiePlaybackOptions);
        if (!IsAnimationLoaded)
        {
            throw new InvalidOperationException("Failed to initialize Lottie Animation");
        }
        // redraw the screen to reflect the changes
        _initCount++;
        IsAnimationPaused = !lottiePlaybackOptions.AutoPlay;

        // setup events
        EventSubscription(true);

        StateHasChanged();
    }

    private void EventSubscription(bool subscribe)
    {
        if (_lottiePlayerModule == null) return;
        if (subscribe)
        {
            _lottiePlayerModule.OnDOMLoaded += HandleDOMLoaded;
            _lottiePlayerModule.OnAnimationReady += HandleAnimationLoaded;
            _lottiePlayerModule.OnComplete += HandleAnimationCompleted;
            _lottiePlayerModule.OnLoopComplete += HandleAnimationLoopCompleted;
            _lottiePlayerModule.OnEnterFrame += HandleCurrentFrameChanged;
        }
        else
        {
            _lottiePlayerModule.OnDOMLoaded -= HandleDOMLoaded;
            _lottiePlayerModule.OnAnimationReady -= HandleAnimationLoaded;
            _lottiePlayerModule.OnComplete -= HandleAnimationCompleted;
            _lottiePlayerModule.OnLoopComplete -= HandleAnimationLoopCompleted;
            _lottiePlayerModule.OnEnterFrame -= HandleCurrentFrameChanged;
        }
    }

    #region Compoinent Lifecycle

    /// <summary>
    /// OnAfterRenderAsync override
    /// </summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // load the Lottie Web Player JS from CDN
            var initModule = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./content/Blazor.Lottie.Player/loadLottieWeb.js");
            var result = await initModule.InvokeAsync<bool>("initialize", AnimationOptions.LottieSourceJs);
            if (!result)
            {
                throw new InvalidOperationException("Failed to initialize Lottie Web Player Module");
            }
            await InitializeLottieModule();
        }
    }

    /// <summary>
    /// Override the SetParametersAsync method to handle component parameters.
    /// </summary>
    /// <param name="parameters">The parameters submitted with the component</param>
    public override Task SetParametersAsync(ParameterView parameters)
    {
        var reInitialize = false;

        var oldSrc = Src;
        var oldAutoPlay = AutoPlay;
        var oldLoop = Loop;
        var oldLoopCount = LoopCount;
        var oldSpeed = AnimationSpeed;
        var oldType = AnimationType;
        var oldDirection = AnimationDirection;
        var oldOptions = AnimationOptions;

        // Apply the parameters to the component
        parameters.SetParameterProperties(this);

        // Check if any key parameters changed
        if (AutoPlay != oldAutoPlay ||
            Loop != oldLoop ||
            AnimationSpeed != oldSpeed ||
            !Equals(AnimationType, oldType) ||
            !Equals(AnimationDirection, oldDirection) ||
            !Equals(AnimationOptions, oldOptions) ||
            Src != oldSrc ||
            LoopCount != oldLoopCount)
        {
            reInitialize = true;
        }

        if (reInitialize)
        {
            return InitializeLottieModule();
        }
        return Task.CompletedTask;
    }


    /// <summary>
    /// Disposes the LottiePlayer component asynchronously, cleaning up resources and references.
    /// </summary>
    /// <returns></returns>
    public async ValueTask DisposeAsync()
    {
        if (_lottiePlayerModule != null)
        {
            // cancel event handlers to prevent memory leaks
            EventSubscription(false);

            await _lottiePlayerModule.DisposeAsync();
            _lottiePlayerModule = null;
        }

        GC.SuppressFinalize(this);
    }

    #endregion
}

