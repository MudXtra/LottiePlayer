using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;
using Moq;

namespace Blazor.Lottie.Player.UnitTests;
public class ParametersLifecycleTests : BunitTest
{
    [Test]
    public async Task Subclass_OnParametersSet_RunsOnKeyParameterChangeAfterInit()
    {
        var js = new FakeJs();
        Context.Services.AddSingleton<IJSRuntime>(js);
        var comp = Context.RenderComponent<CountingLottiePlayer>(p => p.Add(x => x.Src, "https://example.com/a.json"));
        comp.WaitForState(() => js.Created == 1, TimeSpan.FromSeconds(5));
        var before = comp.Instance.OnParametersSetCalls;
        var beforeAsync = comp.Instance.OnParametersSetAsyncCalls;

        await comp.InvokeAsync(() => comp.Instance.SetParametersAsync(
            ParameterView.FromDictionary(new Dictionary<string, object?> { ["Src"] = "https://example.com/b.json" })));

        comp.Instance.OnParametersSetCalls.Should().Be(before + 1, "base lifecycle must run once for a key-parameter change after init");
        comp.Instance.OnParametersSetAsyncCalls.Should().Be(beforeAsync + 1);
        js.Created.Should().Be(2, "the re-init still happens exactly once");
    }

    private sealed class CountingLottiePlayer : LottiePlayer
    {
        public int OnParametersSetCalls, OnParametersSetAsyncCalls;
        protected override void OnParametersSet() { OnParametersSetCalls++; base.OnParametersSet(); }
        protected override Task OnParametersSetAsync() { OnParametersSetAsyncCalls++; return base.OnParametersSetAsync(); }
    }

    private sealed class FakeJs : IJSRuntime
    {
        public int Created;
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
            await Task.Delay(20);
            Interlocked.Increment(ref Created);
            var anim = new Mock<IJSObjectReference>();
            anim.Setup(a => a.InvokeAsync<IJSVoidResult>(It.IsAny<string>(), It.IsAny<object?[]?>()))
                .Returns(new ValueTask<IJSVoidResult>(default(IJSVoidResult)!));
            anim.Setup(a => a.DisposeAsync()).Returns(ValueTask.CompletedTask);
            return anim.Object;
        }
    }
}
