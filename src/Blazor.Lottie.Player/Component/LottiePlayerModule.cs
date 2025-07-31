using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Blazor.Lottie.Player;
/// <summary>
/// Provides functionality to control and manage a Lottie animation player in a Blazor application.
/// </summary>
/// <remarks>This class interacts with a JavaScript module to initialize, play, pause, and stop Lottie animations
/// within a specified HTML element. It also implements asynchronous disposal to clean up resources.</remarks>
public class LottiePlayerModule : IAsyncDisposable
{
    internal readonly IJSRuntime _js;
    internal IJSObjectReference? _module;
    internal readonly ElementReference _elementRef;
    internal DotNetObjectReference<LottiePlayerModule>? _dotNetReference;
    internal bool _isDisposing;
    internal bool _isInitialized;

    /// <summary>
    /// Indicates whether the module can execute javascript based on its current state.
    /// </summary>
    internal bool CanExecute => !(_isDisposing || _js is null || _module is null || !_isInitialized);

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
        _module = await _js.InvokeAsync<IJSObjectReference>("import", "./_content/Blazor.Lottie.Player/lottiePlayerModule.js");
        var result = await _module.InvokeAsync<bool>("initialize", _dotNetReference, _elementRef, lottiePlaybackOptions);
        if (lottiePlaybackOptions.AutoPlay)
        {
            await PlayAsync();
        }
        _isInitialized = result;
        return result;
    }

    #region Actions

    /// <summary>
    /// Plays the Lottie animation.
    /// </summary>
    public async Task<bool> PlayAsync()
    {
        if (!CanExecute) return false;
        return await _module!.InvokeAsync<bool>("play", _elementRef);
    }

    /// <summary>
    /// Pauses the currently playing Lottie animation.
    /// </summary>
    public async Task<bool> PauseAsync()
    {
        if (!CanExecute) return false;
        return await _module!.InvokeAsync<bool>("pause", _elementRef);
    }

    /// <summary>
    /// Stops the currently playing Lottie animation.
    /// </summary>
    public async Task<bool> StopAsync()
    {
        if (!CanExecute) return false;
        return await _module!.InvokeAsync<bool>("stop", _elementRef);
    }

    /// <summary>
    /// Asynchronously sets the speed for the associated animation. 1 is normal speed, 2 is double speed, 0.5 is half speed, etc.
    /// </summary>
    /// <param name="speed">The speed value to set. Must be a non-negative number.</param>
    public async Task<bool> SetSpeedAsync(double speed)
    {
        if (!CanExecute || speed < 0) return false;
        return await _module!.InvokeAsync<bool>("setSpeed", _elementRef, speed);
    }

    /// <summary>
    /// Asynchronously sets the playback direction for the associated animation.
    /// </summary>
    /// <param name="direction">The direction to play the animation. It can be forward or backward.</param>
    public async Task<bool> SetDirectionAsync(LottieAnimationDirection direction)
    {
        if (!CanExecute) return false;
        return await _module!.InvokeAsync<bool>("setDirection", _elementRef, (int)direction);
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
    public event EventHandler<double>? OnEnterFrame;

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
    public void AnimationReadyEvent(LottiePlayerLoadedEventArgs args)
    {
        if (!CanExecute) return;

        OnAnimationReady?.Invoke(this, args);
    }

    /// <summary>
    /// Invokes the <see cref="OnDOMLoaded"/> event when the DOM is fully loaded with the Lottie animation.
    /// </summary>
    /// <param name="args"></param>
    [JSInvokable]
    public void DOMLoadedEvent(LottiePlayerLoadedEventArgs args)
    {
        if (!CanExecute) return;

        OnDOMLoaded?.Invoke(this, args);
    }

    /// <summary>
    /// Invokes the <see cref="OnComplete"/> event when the animation completes.
    /// </summary>
    [JSInvokable]
    public void CompleteEvent()
    {
        if (!CanExecute) return;

        OnComplete?.Invoke(this, true);
    }

    /// <summary>
    /// Invokes the <see cref="OnLoopComplete"/> event when a loop completes.
    /// </summary>
    [JSInvokable]
    public void LoopCompleteEvent()
    {
        if (!CanExecute) return;

        OnLoopComplete?.Invoke(this, true);
    }

    /// <summary>
    /// Invokes the <see cref="OnEnterFrame"/> event with the specified arguments.
    /// </summary>
    [JSInvokable]
    public void EnterFrameEvent(double args)
    {
        if (!CanExecute) return;

        OnEnterFrame?.Invoke(this, args);
    }

    #endregion

    /// <summary>
    /// Disposes the Lottie player module and cleans up resources.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        _isDisposing = true;
        if (_module is not null)
        {
            try
            {
                await _module.InvokeVoidAsync("destroy", _elementRef);
            }
            catch (JSException)
            {
                // Ignore JS exceptions during dispose, as the module may already be disposed.
            }
            await _module.DisposeAsync();
            _module = null;
        }
        if (_dotNetReference is not null)
        {
            _dotNetReference.Dispose();
            _dotNetReference = null;
        }
        GC.SuppressFinalize(this);
    }
}
