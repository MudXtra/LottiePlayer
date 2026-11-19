using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;
using Moq;

namespace Blazor.Lottie.Player.UnitTests;
public class CoalesceReinitTests : BunitTest
{
    [Test]
    public async Task Component_RapidSrcChangesDuringInFlightReinit_CoalesceToOneExtraRebuild()
    {
        var js = new CountingJs();
        Context.Services.AddSingleton<IJSRuntime>(js);
        var comp = Context.RenderComponent<LottiePlayer>(p => p.Add(x => x.Src, "https://example.com/a.json"));
        comp.WaitForState(() => js.Created == 1, TimeSpan.FromSeconds(5));

        Task Change(string src) => comp.InvokeAsync(() => comp.Instance.SetParametersAsync(
            ParameterView.FromDictionary(new Dictionary<string, object?> { ["Src"] = src })));

        var first = Change("https://example.com/b.json");
        // the b re-init holds the lock once it has torn down the first animation; it then waits on ReleaseB
        await WaitUntilAsync(() => js.Destroyed == 1);
        var second = Change("https://example.com/c.json");
        var third = Change("https://example.com/d.json");
        js.ReleaseB.SetResult();
        await Task.WhenAll(first, second, third).WaitAsync(TimeSpan.FromSeconds(10));

        // initial + the in-flight rebuild + at most one coalesced rebuild
        js.Created.Should().BeLessThanOrEqualTo(3);
        js.Paths.Last().Should().Be("https://example.com/d.json", "the last animation reflects the final parameters");
        (js.Created - js.Destroyed).Should().Be(1, "only the live animation is left");
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (!condition())
        {
            if (DateTime.UtcNow > deadline) throw new TimeoutException("condition not reached");
            await Task.Delay(5);
        }
    }

    private sealed class CountingJs : IJSRuntime
    {
        public int Created, Destroyed;
        private int _inits;
        public readonly TaskCompletionSource ReleaseB = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly System.Collections.Concurrent.ConcurrentQueue<string?> PathQueue = new();
        public IEnumerable<string?> Paths => PathQueue;
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => InvokeAsync<TValue>(identifier, CancellationToken.None, args);
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken ct, object?[]? args)
        {
            var module = new Mock<IJSObjectReference>();
            module.Setup(m => m.InvokeAsync<bool>("initialize", It.IsAny<object?[]?>())).Returns(new ValueTask<bool>(true));
            module.Setup(m => m.InvokeAsync<IJSObjectReference?>("initialize", It.IsAny<object?[]?>()))
                .Returns((string _, object?[]? a) => new ValueTask<IJSObjectReference?>(Init(((LottiePlaybackOptions)a![2]!).Path)));
            return new ValueTask<TValue>((TValue)module.Object);
        }
        private async Task<IJSObjectReference?> Init(string? path)
        {
            if (Interlocked.Increment(ref _inits) == 2) // the first re-init (b) blocks until the test releases it
            {
                await ReleaseB.Task;
            }
            else await Task.Delay(100);
            Interlocked.Increment(ref Created);
            PathQueue.Enqueue(path);
            var anim = new Mock<IJSObjectReference>();
            anim.Setup(a => a.InvokeAsync<IJSVoidResult>(It.IsAny<string>(), It.IsAny<object?[]?>()))
                .Returns((string id, object?[]? _) => new ValueTask<IJSVoidResult>(Invoke(id)));
            anim.Setup(a => a.DisposeAsync()).Returns(ValueTask.CompletedTask);
            return anim.Object;
        }
        private async Task<IJSVoidResult> Invoke(string identifier)
        {
            await Task.Delay(20);
            if (identifier == "destroy") Interlocked.Increment(ref Destroyed);
            return default!;
        }
    }
}
