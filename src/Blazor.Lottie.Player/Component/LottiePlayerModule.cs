using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Blazor.Lottie.Player;
/// <summary>
/// Provides functionality to control and manage a Lottie animation player in a Blazor application. 
/// </summary>
/// <remarks>
/// This class interacts with a JavaScript module to initialize, play, pause, and stop Lottie animations
/// within a specified HTML element. It also implements asynchronous disposal to clean up resources.
/// <para><b>WARNING:</b> This class does not load lottie javascript files, you must load those yourself or use the component.</para>
/// </remarks>
public class LottiePlayerModule : IAsyncDisposable
{
    internal readonly IJSRuntime _js;
    internal IJSObjectReference? _lottieAnimationRef;
    internal readonly ElementReference _elementRef;
    internal DotNetObjectReference<LottiePlayerModule>? _dotNetReference;
    internal bool _isDisposing;

    /// <summary>
    /// Indicates whether the module can execute javascript based on its current state.
    /// </summary>
    internal bool CanExecute => !(_isDisposing || _js is null || _lottieAnimationRef is null);

    /// <summary>
    /// The constructor for the LottiePlayerModule class.
    /// </summary>
    /// <param name="js">An active IJSRuntime</param>
    /// <param name="elementRef">The reference to the HTML element that the Lottie animation will be rendered in.</param>
    public LottiePlayerModule(IJSRuntime js, ElementReference elementRef)
    {
        _dotNetReference = DotNetObjectReference.Create(this);
        _js = js;
        _elementRef = elementRef;
    }

    /// <summary>
    /// Initializes the Lottie player with the specified playback options.
    /// </summary>
    /// <remarks>This method imports the required JavaScript module and invokes the initialization logic for
    /// the Lottie player. It also ensures that the instance is not disposing and that the JavaScript runtime is available before
    /// loading the module.</remarks>
    /// <param name="lottiePlaybackOptions">The playback options to configure the Lottie player.</param>
    /// <returns><see langword="true"/> if the initialization is successful; otherwise, <see langword="false"/>.</returns>
    public async Task<bool> InitializeAsync(LottiePlaybackOptions lottiePlaybackOptions)
    {
        if (_isDisposing || _js is null) return false;
        var _module = await _js.InvokeAsync<IJSObjectReference?>("import", "./_content/Blazor.Lottie.Player/lottiePlayerModule.js");
        _lottieAnimationRef = await _module!.InvokeAsync<IJSObjectReference?>("initialize", _dotNetReference, _elementRef, lottiePlaybackOptions);
        if (lottiePlaybackOptions.AutoPlay)
        {
            await PlayAsync();
        }
        return _lottieAnimationRef != null;
    }

    #region Actions

    /// <summary>
    /// Plays the Lottie animation.
    /// </summary>
    public ValueTask PlayAsync()
    {
        if (!CanExecute) return ValueTask.CompletedTask;
        return _lottieAnimationRef!.InvokeVoidAsync("play");
    }

    /// <summary>
    /// Pauses the currently playing Lottie animation.
    /// </summary>
    public ValueTask PauseAsync()
    {
        if (!CanExecute) return ValueTask.CompletedTask;
        return _lottieAnimationRef!.InvokeVoidAsync("pause");
    }

    /// <summary>
    /// Stops the currently playing Lottie animation.
    /// </summary>
    public ValueTask StopAsync()
    {
        if (!CanExecute) return ValueTask.CompletedTask;
        return _lottieAnimationRef!.InvokeVoidAsync("stop");
    }

    /// <summary>
    /// Moves the animation to the specified frame and starts playback from that point.
    /// </summary>
    /// <param name="frame">The frame number to move the animation to. Must be a non-negative value.</param>
    /// <param name="force">A boolean value indicating whether to force the animation to move to the specified frame  even if it is already
    /// playing.  <see langword="true"/> to force the action; otherwise, <see langword="false"/>.</param>
    public ValueTask GoToAndPlay(double frame, bool force = false)
    {
        if (!CanExecute) return ValueTask.CompletedTask;
        return _lottieAnimationRef!.InvokeVoidAsync("goToAndPlay", frame, force);
    }

    /// <summary>
    /// Moves the animation to the specified frame and stops playback at that point.
    /// </summary>
    /// <param name="frame">The frame number to move the animation to. Must be a non-negative value.</param>
    /// <param name="force">A boolean value indicating whether to force the animation to move to the specified frame  even if it is already
    /// playing.  <see langword="true"/> to force the action; otherwise, <see langword="false"/>.</param>
    public ValueTask GoToAndStop(double frame, bool force = false)
    {
        if (!CanExecute) return ValueTask.CompletedTask;
        return _lottieAnimationRef!.InvokeVoidAsync("goToAndStop", frame, force);
    }

    /// <summary>
    /// Asynchronously sets the speed for the associated animation. 1 is normal speed, 2 is double speed, 0.5 is half speed, etc.
    /// </summary>
    /// <param name="speed">The speed value to set. Must be a non-negative number.</param>
    public ValueTask SetSpeedAsync(double speed)
    {
        if (!CanExecute || speed < 0) return ValueTask.CompletedTask;
        return _lottieAnimationRef!.InvokeVoidAsync("setSpeed", speed);
    }

    /// <summary>
    /// Asynchronously sets the playback direction for the associated animation.
    /// </summary>
    /// <param name="direction">The direction to play the animation. It can be forward or backward.</param>
    public ValueTask SetDirectionAsync(LottieAnimationDirection direction)
    {
        if (!CanExecute) return ValueTask.CompletedTask;
        return _lottieAnimationRef!.InvokeVoidAsync("setDirection", (int)direction);
    }

    #endregion

    #region Events

    /// <summary>
    /// Triggers when the Lottie animation is ready to be played and loaded by the DOM.
    /// </summary>
    public event EventHandler<LottiePlayerLoadedEventArgs>? OnAnimationReady;

    /// <summary>
    /// Triggers when the Lottie animation has fully loaded in the DOM.
    /// </summary>
    public event EventHandler<LottiePlayerLoadedEventArgs>? OnDOMLoaded;

    /// <summary>
    /// Triggers when the Lottie animation enters a new frame.
    /// </summary>
    public event EventHandler<LottiePlayerEventFrameArgs>? OnEnterFrame;

    /// <summary>
    /// Triggers when the Lottie animation completes playing.
    /// </summary>
    public event EventHandler<bool>? OnComplete;

    /// <summary>
    /// Triggers when the Lottie animation completes a loop.
    /// </summary>
    public event EventHandler<bool>? OnLoopComplete;

    /// <summary>
    /// Invokes the <see cref="OnAnimationReady"/> event when the animation is ready. (DOMLoaded)
    /// </summary>
    [JSInvokable]
    public Task AnimationReadyEvent(LottiePlayerLoadedEventArgs args)
    {
        if (!CanExecute) return Task.CompletedTask;

        OnAnimationReady?.Invoke(this, args);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Invokes the <see cref="OnDOMLoaded"/> event when the DOM is fully loaded with the Lottie animation.
    /// </summary>
    /// <param name="args"></param>
    [JSInvokable]
    public Task DOMLoadedEvent(LottiePlayerLoadedEventArgs args)
    {
        if (!CanExecute) return Task.CompletedTask;

        OnDOMLoaded?.Invoke(this, args);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Invokes the <see cref="OnComplete"/> event when the animation completes.
    /// </summary>
    /// <remarks>
    /// This event is triggered when the animation has finished playing all frames and loops, if applicable. If it's set to loop
    /// indefinitely, this event will never be triggered.
    /// </remarks>
    [JSInvokable]
    public Task CompleteEvent()
    {
        if (!CanExecute) return Task.CompletedTask;

        OnComplete?.Invoke(this, true);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Invokes the <see cref="OnLoopComplete"/> event when a loop completes.
    /// </summary>
    [JSInvokable]
    public Task LoopCompleteEvent()
    {
        if (!CanExecute) return Task.CompletedTask;

        OnLoopComplete?.Invoke(this, true);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Invokes the <see cref="OnEnterFrame"/> event with the specified arguments.
    /// </summary>
    [JSInvokable]
    public Task EnterFrameEvent(LottiePlayerEventFrameArgs args)
    {
        if (!CanExecute) return Task.CompletedTask;

        OnEnterFrame?.Invoke(this, args);
        return Task.CompletedTask;
    }

    #endregion

    /// <summary>
    /// Disposes the Lottie player module and cleans up resources.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        _isDisposing = true;
        if (_lottieAnimationRef is not null)
        {
            try
            {
                await _lottieAnimationRef.InvokeVoidAsync("destroy");
            }
            catch (JSException)
            {
                // Ignore JS exceptions during dispose, as the module may already be disposed.
            }
            await _lottieAnimationRef.DisposeAsync();
            _lottieAnimationRef = null;
        }
        if (_dotNetReference is not null)
        {
            _dotNetReference.Dispose();
            _dotNetReference = null;
        }
        GC.SuppressFinalize(this);
    }
}
