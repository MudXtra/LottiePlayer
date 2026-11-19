using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;
using Moq;

namespace Blazor.Lottie.Player.UnitTests;
public class DisposalDuringInitTests : BunitTest
{
    [Test]
    public async Task Component_DisposeDuringReinit_DestroysEveryAnimation()
    {
        var js = new CountingJs();
        Context.Services.AddSingleton<IJSRuntime>(js);
        var comp = Context.RenderComponent<LottiePlayer>(p => p.Add(x => x.Src, "https://example.com/a.json"));
        comp.WaitForState(() => js.Created == 1, TimeSpan.FromSeconds(5));

        var reinit = comp.InvokeAsync(() => comp.Instance.SetParametersAsync(
            ParameterView.FromDictionary(new Dictionary<string, object?> { ["Src"] = "https://example.com/b.json" })));
        await Task.Delay(30); // reinit is now awaiting the new animation's initialize
        await comp.InvokeAsync(() => comp.Instance.DisposeAsync());
        await reinit;

        js.Destroyed.Should().Be(js.Created, "every created animation should be destroyed after component disposal");
    }

    [Test]
    public async Task Module_DisposeDuringInitialize_DestroysTheReturnedAnimation()
    {
        var js = new CountingJs();
        var module = new LottiePlayerModule(js, default);

        var init = module.InitializeAsync(new LottiePlaybackOptions { AutoPlay = false });
        await Task.Delay(30); // JS initialize is in flight
        await module.DisposeAsync();
        var result = await init;

        result.Should().BeFalse();
        js.Created.Should().Be(1);
        js.Destroyed.Should().Be(js.Created);
    }

    [Test]
    public async Task Component_ScriptLoadFailure_IsNotClearedByTogglingSrc()
    {
        var moduleMock = Context.JSInterop.SetupModule("./_content/Blazor.Lottie.Player/loadLottieWeb.js");
        moduleMock.Setup<bool>("initialize", _ => true).SetResult(false);

        var comp = Context.RenderComponent<LottiePlayer>(p => p.Add(x => x.Src, "https://example.com/a.json"));
        comp.Markup.Should().Contain("Failed to load the Lottie Web Player script.");

        comp.SetParametersAndRender(p => p.Add(x => x.Src, "not-json.txt"));
        comp.SetParametersAndRender(p => p.Add(x => x.Src, "https://example.com/b.json"));

        comp.Markup.Should().Contain("Failed to load the Lottie Web Player script.");
        comp.Markup.Should().NotContain("Src must be a valid JSON");
        await Task.CompletedTask;
    }

    private sealed class CountingJs : IJSRuntime
    {
        public int Created, Destroyed;
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => InvokeAsync<TValue>(identifier, CancellationToken.None, args);
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken ct, object?[]? args)
        {
            var module = new Mock<IJSObjectReference>();
            module.Setup(m => m.InvokeAsync<bool>("initialize", It.IsAny<object?[]?>())).Returns(new ValueTask<bool>(true));
            module.Setup(m => m.InvokeAsync<IJSObjectReference?>("initialize", It.IsAny<object?[]?>()))
                .Returns(() => new ValueTask<IJSObjectReference?>(Init()));
            return new ValueTask<TValue>((TValue)module.Object);
        }
        private async Task<IJSObjectReference?> Init()
        {
            await Task.Delay(100);
            Interlocked.Increment(ref Created);
            var anim = new Mock<IJSObjectReference>();
            anim.Setup(a => a.InvokeAsync<IJSVoidResult>(It.IsAny<string>(), It.IsAny<object?[]?>()))
                .Callback<string, object?[]?>((id, _) => { if (id == "destroy") Interlocked.Increment(ref Destroyed); })
                .Returns(new ValueTask<IJSVoidResult>(default(IJSVoidResult)!));
            anim.Setup(a => a.DisposeAsync()).Returns(ValueTask.CompletedTask);
            return anim.Object;
        }
    }
}
