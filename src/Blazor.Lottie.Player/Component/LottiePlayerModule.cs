using Blazor.Lottie.Player.Serialization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Blazor.Lottie.Player;
public class LottiePlayerModule : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private IJSObjectReference? _module;
    private ElementReference _elementRef;
    private DotNetObjectReference<LottiePlayerModule>? _dotNetReference;
    internal bool _isDisposing;

    public LottiePlayerModule(IJSRuntime js, ElementReference elementRef)
    {
        _dotNetReference = DotNetObjectReference.Create(this);
        _js = js;
        _elementRef = elementRef;
    }

    public async Task<bool> InitializeAsync(LottiePlaybackOptions lottiePlaybackOptions)
    {
        if (_isDisposing || _js is null) return false;
        _module = await _js.InvokeAsync<IJSObjectReference>("import", "./content/Blazor.Lottie.Player/lottiePlayerModule.js");
        var result = await _module.InvokeAsync<bool>("initialize", _dotNetReference, _elementRef, lottiePlaybackOptions);
        return result;
    }

    public async Task<bool> PlayAsync()
    {
        if (_isDisposing || _js is null || _module is null) return false;
        return await _module.InvokeAsync<bool>("play", _elementRef);
    }

    public async Task<bool> PauseAsync()
    {
        if (_isDisposing || _js is null || _module is null) return false;
        return await _module.InvokeAsync<bool>("pause", _elementRef);
    }

    public async Task<bool> StopAsync()
    {
        if (_isDisposing || _js is null || _module is null) return false;
        return await _module.InvokeAsync<bool>("stop", _elementRef);
    }

    public async ValueTask DisposeAsync()
    {
        _isDisposing = true;
        if (_module is not null)
        {
            try
            {
                await _module.InvokeVoidAsync("dispose", _elementRef);
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
    }
}
