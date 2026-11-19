using System.Reflection;
using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop.Infrastructure;
using Moq;

namespace Blazor.Lottie.Player.UnitTests
{
    [TestFixture]
    public class LottieTests : BunitTest
    {
        private const string LottieSrc = "https://raw.githubusercontent.com/MudXtra/LottiePlayer/refs/heads/main/src/Blazor.Lottie.Player.Docs.WASM/wwwroot/lottie/newAnimation.json";

        [SetUp]
        public override void Setup()
        {
            base.Setup();

            // Setup the import call to return a mock module
            var moduleMock = Context.JSInterop.SetupModule("./_content/Blazor.Lottie.Player/loadLottieWeb.js");
            // Setup the initialize call to return true
            moduleMock.Setup<bool>("initialize", _ => true).SetResult(true);

            var lottieModuleMock = Context.JSInterop.SetupModule("./_content/Blazor.Lottie.Player/lottiePlayerModule.js");
            var animationModuleMock = new Mock<IJSObjectReference>();
            lottieModuleMock.SetupVoid("initialize");

            lottieModuleMock.Setup<bool>("play", _ => true).SetResult(true);
            lottieModuleMock.Setup<bool>("pause", _ => true).SetResult(true);
            lottieModuleMock.Setup<bool>("stop", _ => true).SetResult(true);
            lottieModuleMock.Setup<bool>("destroy", _ => true).SetResult(true);
            lottieModuleMock.Setup<bool>("setSpeed", _ => true).SetResult(true);
            lottieModuleMock.Setup<bool>("setDirection", _ => true).SetResult(true);
        }

        [Test]
        public async Task LottieComponentRendersCorrectly()
        {
            // Arrange
            var comp = Context.RenderComponent<LottiePlayer>(parameters => parameters
                .Add(p => p.Src, LottieSrc)
                .Add(p => p.Class, "test-class")
                .Add(p => p.Style, "width: 100%; height: 100%;")
            );
            // Act
            var element = comp.Find($"#{comp.Instance.ElementId}");
            element.Should().NotBeNull();

            // Assert Defaults
            comp.Instance.AutoPlay.Should().BeTrue();
            comp.Instance.Loop.Should().BeTrue();
            comp.Instance.LoopCount.Should().Be(0);
            comp.Instance.AnimationType.Should().Be(LottieAnimationType.Svg);
            comp.Instance.AnimationSpeed.Should().Be(1.0);
            comp.Instance.AnimationDirection.Should().Be(LottieAnimationDirection.Forward);
            var options = comp.Instance.AnimationOptions;
            options.Should().NotBeNull();
            options.LottieSourceJs.Should().Be("https://cdnjs.cloudflare.com/ajax/libs/lottie-web/5.13.0/lottie.min.js");
            options.AnimationQuality.Should().Be(LottieAnimationQuality.High);
            options.SmoothFrameInterpolation.Should().BeTrue();

            // get and invoke private void HandleAnimationReady(object? sender, LottiePlayerLoadedEventArgs args)
            var method = typeof(LottiePlayer).GetMethod("HandleAnimationReady", BindingFlags.NonPublic | BindingFlags.Instance);
            method.Should().NotBeNull();
            // Invoke the method with a mock LottiePlayerLoadedEventArgs
            var args = new LottiePlayerLoadedEventArgs
            {
                ElementId = comp.Instance.ElementId,
                CurrentFrame = 0.0,
                TotalFrames = 100.0,
            };
            await comp.InvokeAsync(() => method.Invoke(comp.Instance, [this, args]));

            comp.Instance.IsAnimationLoaded.Should().BeTrue();
            comp.Instance.IsAnimationPaused.Should().Be(!comp.Instance.AutoPlay);
            comp.Instance.CurrentAnimationFrame.Should().Be(0.0);
        }

        [Test]
        public void LottiePlayer_RendersUserAttributes()
        {
            var comp = Context.RenderComponent<LottiePlayer>(parameters => parameters
                .Add(p => p.Src, "https://example.com/lottie.json")
            );
            var element = comp.Find($"#{comp.Instance.ElementId}");

            // Check default attributes
            element.HasAttribute("data-test").Should().BeFalse();
            element.GetAttribute("role").Should().Be("animation");
            element.GetAttribute("aria-label").Should().Be("Lottie Animation");

            comp.SetParametersAndRender(parameters => parameters
                .Add(p => p.UserAttributes, new Dictionary<string, object?>
                {
                    { "data-test", "abc" },
                    { "aria-label", "lottie" }
                })
            );

            element = comp.Find($"#{comp.Instance.ElementId}");

            element.HasAttribute("data-test").Should().BeTrue();
            element.GetAttribute("data-test").Should().Be("abc");
            element.GetAttribute("aria-label").Should().Be("lottie");
            element.GetAttribute("role").Should().Be("animation");
        }

        [Test]
        public void LottiePlayer_ClassnameAndStylename_NullOrWhitespace()
        {
            var comp = Context.RenderComponent<LottiePlayer>(parameters => parameters
                .Add(p => p.Src, "https://example.com/lottie.json")
                .Add(p => p.Class, null)
                .Add(p => p.Style, "   ")
            );
            var element = comp.Find($"#{comp.Instance.ElementId}");
            element.ClassList.Should().Contain("blazor-lottie-player");
            element.GetAttribute("style").Should().BeNullOrWhiteSpace();
        }

        [Test]
        public async Task LottiePlayer_PublicMethods_ThrowIfModuleNull()
        {
            var comp = Context.RenderComponent<LottiePlayer>(parameters => parameters
                .Add(p => p.Src, "https://example.com/lottie.json")
            );
            // Simulate module disposal
            comp.Instance.LottiePlayerModule.Should().NotBeNull();
            await comp.InvokeAsync(() => comp.Instance.DisposeAsync());
            comp.Instance.LottiePlayerModule.Should().BeNull();

            await FluentActions.Awaiting(async () => await comp.Instance.PlayAnimationAsync()).Should().ThrowAsync<InvalidOperationException>();
            await FluentActions.Awaiting(async () => await comp.Instance.PauseAnimationAsync()).Should().ThrowAsync<InvalidOperationException>();
            await FluentActions.Awaiting(async () => await comp.Instance.StopAnimationAsync()).Should().ThrowAsync<InvalidOperationException>();
            await FluentActions.Awaiting(async () => await comp.Instance.SetSpeedAsync(1.0)).Should().ThrowAsync<InvalidOperationException>();
            await FluentActions.Awaiting(async () => await comp.Instance.SetDirectionAsync(LottieAnimationDirection.Forward)).Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public void LottiePlayer_ThrowsIfJsModuleFailsToLoad()
        {
            // Both the CDN and the local fallback fail: bounded attempts, failure message, no throw, no render loop.
            var moduleMock = Context.JSInterop.SetupModule("./_content/Blazor.Lottie.Player/loadLottieWeb.js");
            moduleMock.Setup<bool>("initialize", _ => true).SetResult(false);

            var comp = Context.RenderComponent<LottiePlayer>(parameters => parameters
                .Add(p => p.Src, "https://example.com/lottie.json")
            );

            comp.Markup.Should().Contain("Failed to load the Lottie Web Player script");
            comp.Instance.LottiePlayerModule.Should().BeNull();
            var calls = moduleMock.Invocations["initialize"].ToList();
            calls.Should().HaveCount(2);
            calls[0].Arguments[0].Should().Be("https://cdnjs.cloudflare.com/ajax/libs/lottie-web/5.13.0/lottie.min.js");
            calls[1].Arguments[0].Should().Be("./_content/Blazor.Lottie.Player/lottie.min.js");
        }

        [Test]
        public void LottiePlayer_FallsBackToLocalScriptWhenCdnFails()
        {
            var moduleMock = Context.JSInterop.SetupModule("./_content/Blazor.Lottie.Player/loadLottieWeb.js");
            moduleMock.Setup<bool>("initialize", a => (string)a.Arguments[0]! != "./_content/Blazor.Lottie.Player/lottie.min.js").SetResult(false);
            moduleMock.Setup<bool>("initialize", a => (string)a.Arguments[0]! == "./_content/Blazor.Lottie.Player/lottie.min.js").SetResult(true);

            var comp = Context.RenderComponent<LottiePlayer>(parameters => parameters
                .Add(p => p.Src, "https://example.com/lottie.json")
            );

            comp.Markup.Should().NotContain("Failed to load");
            comp.Instance.LottiePlayerModule.Should().NotBeNull();
            moduleMock.Invocations["initialize"].Should().HaveCount(2);
        }

        [Test]
        public void LottiePlayer_InitShouldReinitReasonably()
        {
            var comp = Context.RenderComponent<LottiePlayer>(parameters => parameters
                .Add(p => p.Src, LottieSrc)
            );

            var field = typeof(LottiePlayer).GetField("_initCount", BindingFlags.NonPublic | BindingFlags.Instance);
            field.Should().NotBeNull();
            var initCount = (int)field.GetValue(comp.Instance)!;
            initCount.Should().Be(1);

            // Changing these parameters should not reinitialize
            comp.SetParametersAndRender(parameters => parameters
                .Add(p => p.Class, "test-class")
                .Add(p => p.Style, "width: 100%; height: 100%;")
                .Add(p => p.UserAttributes, new Dictionary<string, object?>
                {
                    { "data-test", "test-value" }
                })
            );
            initCount = (int)field.GetValue(comp.Instance)!;
            initCount.Should().Be(1);

            // Change any of these parameters should reinitialize but only once per parameter submission
            comp.SetParametersAndRender(parameters => parameters
                .Add(p => p.Src, "https://assets2.lottiefiles.com/packages/lf20_8j3q1k.json")
                .Add(p => p.AutoPlay, false)
                .Add(p => p.Loop, false)
                .Add(p => p.LoopCount, 5)
                .Add(p => p.AnimationType, LottieAnimationType.Canvas)
                .Add(p => p.AnimationSpeed, 0.5)
                .Add(p => p.AnimationDirection, LottieAnimationDirection.Reverse)
            );
            initCount = (int)field.GetValue(comp.Instance)!;
            initCount.Should().Be(2);

            // Change a single parameter on the list should reinitialize
            comp.SetParametersAndRender(parameters => parameters
                .Add(p => p.AnimationType, LottieAnimationType.Html)
            );
            initCount = (int)field.GetValue(comp.Instance)!;
            initCount.Should().Be(3);

            var options = new LottieAnimationOptions
            {
                AnimationQuality = LottieAnimationQuality.Medium
            };
            comp.Instance.AnimationOptions.Should().NotBe(options.AnimationQuality);

            // Change a options class should reinitialize
            comp.SetParametersAndRender(parameters => parameters
                .Add(p => p.AnimationOptions, options)
            );
            initCount = (int)field.GetValue(comp.Instance)!;
            initCount.Should().Be(4);
        }

        [Test]
        public async Task LottiePlayer_PublicMethods()
        {
            var comp = Context.RenderComponent<LottiePlayer>(parameters => parameters
                .Add(p => p.Src, LottieSrc)
            );
            // Check public methods
            comp.Instance.AutoPlay.Should().BeTrue();
            comp.Instance.IsAnimationLoaded.Should().BeTrue();
            comp.Instance.IsAnimationPaused.Should().BeFalse();

            await comp.InvokeAsync(async () => await comp.Instance.PauseAnimationAsync());
            comp.Instance.IsAnimationPaused.Should().BeTrue();

            await comp.InvokeAsync(async () => await comp.Instance.PlayAnimationAsync());
            comp.Instance.IsAnimationPaused.Should().BeFalse();

            await comp.InvokeAsync(async () => await comp.Instance.SetSpeedAsync(2.0));

            await comp.InvokeAsync(async () => await comp.Instance.SetDirectionAsync(LottieAnimationDirection.Reverse));

            await comp.InvokeAsync(async () => await comp.Instance.StopAnimationAsync());
            comp.Instance.IsAnimationPaused.Should().BeFalse();
            comp.Instance.IsAnimationStopped.Should().BeTrue();
        }

        [Test]
        public void LottiePlayer_Events()
        {
            bool handledom = false,
                 handleenterframe = false,
                 handlecomplete = false,
                 handleloopcomplete = false,
                 handleanimationready = false;

            var comp = Context.RenderComponent<LottiePlayer>(parameters => parameters
                .Add(p => p.Src, LottieSrc)
                .Add(p => p.DOMLoaded, (() => handledom = true))
                .Add(p => p.AnimationReady, () => handleenterframe = true)
                .Add(p => p.AnimationCompleted, (isLooping) => handlecomplete = true)
                .Add(p => p.AnimationLoopCompleted, (isLooping) => handleloopcomplete = true)
                .Add(p => p.CurrentFrameChanged, (args) => handleanimationready = true)
            );
            // invoke the methods that invoke the events
            var domloadedMethod = typeof(LottiePlayer).GetMethod("HandleDOMLoaded", BindingFlags.NonPublic | BindingFlags.Instance);
            comp.InvokeAsync(() => domloadedMethod!.Invoke(comp.Instance, [this, new LottiePlayerLoadedEventArgs()]));
            var animationReadyMethod = typeof(LottiePlayer).GetMethod("HandleAnimationReady", BindingFlags.NonPublic | BindingFlags.Instance);
            comp.InvokeAsync(() => animationReadyMethod!.Invoke(comp.Instance, [this, new LottiePlayerLoadedEventArgs()]));
            var animationCompletedMethod = typeof(LottiePlayer).GetMethod("HandleAnimationCompleted", BindingFlags.NonPublic | BindingFlags.Instance);
            comp.InvokeAsync(() => animationCompletedMethod!.Invoke(comp.Instance, [this, true]));
            var loopCompleteMethod = typeof(LottiePlayer).GetMethod("HandleAnimationLoopCompleted", BindingFlags.NonPublic | BindingFlags.Instance);
            comp.InvokeAsync(() => loopCompleteMethod!.Invoke(comp.Instance, [this, true]));
            var currentFrameChangedMethod = typeof(LottiePlayer).GetMethod("HandleCurrentFrameChanged", BindingFlags.NonPublic | BindingFlags.Instance);
            comp.InvokeAsync(() => currentFrameChangedMethod!.Invoke(comp.Instance, [this, new LottiePlayerEventFrameArgs()]));

            // Check event invocations
            handledom.Should().BeTrue();
            handleenterframe.Should().BeTrue();
            handlecomplete.Should().BeTrue();
            handleloopcomplete.Should().BeTrue();
            handleanimationready.Should().BeTrue();
        }

#pragma warning disable CS8619
        private static IEnumerable<object[]> CurrentFrameChangeFuncCases()
        {
            yield return new object[] { (Func<double, bool>)(frame => frame % 10 == 0), 10 };
            yield return new object?[] { null, 5 };
        }
#pragma warning restore CS8619

        [Test, TestCaseSource(nameof(CurrentFrameChangeFuncCases))]
        public void Lottie_CurrentFrameChanged(object? funcObj, int maxFrames)
        {
            var currentFrameChangeFunc = funcObj as Func<double, bool>;
            var frameChangedCount = 0;

            var comp = Context.RenderComponent<LottiePlayer>(parameters => parameters
                .Add(p => p.Src, "https://assets2.lottiefiles.com/packages/lf20_8j3q1k.json")
                .Add(p => p.CurrentFrameChangeFunc, currentFrameChangeFunc)
                .Add(p => p.CurrentFrameChanged, (Action<LottiePlayerEventFrameArgs>)(args =>
                {
                    frameChangedCount++;
                }))
            );

            if (currentFrameChangeFunc != null)
            {
                for (int frame = 0; frame < maxFrames; frame++)
                {
                    if (currentFrameChangeFunc(frame))
                    {
                        // Your logic here
                    }
                }
            }
        }

        [Test]
        public void LottiePlayer_CurrentFrameChangeFunc_False_DoesNotInvokeEvent()
        {
            bool eventInvoked = false;
            var comp = Context.RenderComponent<LottiePlayer>(parameters => parameters
                .Add(p => p.Src, "https://example.com/lottie.json")
                .Add(p => p.CurrentFrameChangeFunc, (Func<double, bool>)(_ => false))
                .Add(p => p.CurrentFrameChanged, (Action<LottiePlayerEventFrameArgs>)(_ => eventInvoked = true))
            );
            // Use reflection to invoke the private handler
            var method = typeof(LottiePlayer).GetMethod("HandleCurrentFrameChanged", BindingFlags.NonPublic | BindingFlags.Instance);
            method!.Invoke(comp.Instance, [null, new LottiePlayerEventFrameArgs { CurrentTime = 42.0 }]);
            eventInvoked.Should().BeFalse();
        }

        [Test]
        public async Task LottiePlayer_DisposeAsync_Idempotent()
        {
            var comp = Context.RenderComponent<LottiePlayer>(parameters => parameters
                .Add(p => p.Src, "https://example.com/lottie.json")
            );
            await comp.Instance.DisposeAsync();
            // Should not throw or double-dispose
            await comp.Instance.DisposeAsync();
        }

        [Test]
        public async Task LottiePlayer_InitCount_AfterDisposeAndReinit()
        {
            var comp = Context.RenderComponent<LottiePlayer>(parameters => parameters
                .Add(p => p.Src, "https://example.com/lottie.json")
            );
            var field = typeof(LottiePlayer).GetField("_initCount", BindingFlags.NonPublic | BindingFlags.Instance);
            int initCount = (int)field!.GetValue(comp.Instance)!;
            await comp.Instance.DisposeAsync();
            // Re-initialize by changing a key parameter
            comp.SetParametersAndRender(p => p.Add(x => x.Src, "https://example.com/other.json"));
            int newInitCount = (int)field.GetValue(comp.Instance)!;
            newInitCount.Should().Be(initCount + 1);
        }

        [Test]
        public void LottiePlayer_ClearsSrcFailMessageWhenSrcBecomesValid()
        {
            var comp = Context.RenderComponent<LottiePlayer>(parameters => parameters
                .Add(p => p.Src, "not-a-json-path.txt")
            );
            comp.Markup.Should().Contain("Src must be a valid JSON file path");

            comp.SetParametersAndRender(parameters => parameters
                .Add(p => p.Src, "https://example.com/lottie.json")
            );

            comp.Markup.Should().NotContain("Src must be a valid JSON file path");
        }

        [Test]
        public async Task LottiePlayer_SerializesReinitialization()
        {
            var js = new OverlapTrackingJsRuntime();
            Context.Services.AddSingleton<IJSRuntime>(js);

            var comp = Context.RenderComponent<LottiePlayer>(parameters => parameters
                .Add(p => p.Src, "https://example.com/a.json")
            );
            comp.WaitForState(() => js.InitializeCompleted == 1, TimeSpan.FromSeconds(5));

            // two quick parameter changes without awaiting the first
            var first = comp.InvokeAsync(() => comp.Instance.SetParametersAsync(
                ParameterView.FromDictionary(new Dictionary<string, object?> { ["Src"] = "https://example.com/b.json" })));
            var second = comp.InvokeAsync(() => comp.Instance.SetParametersAsync(
                ParameterView.FromDictionary(new Dictionary<string, object?> { ["Src"] = "https://example.com/c.json" })));
            await Task.WhenAll(first, second);

            js.MaxConcurrentInitializations.Should().Be(1);
            js.InitializeCompleted.Should().Be(3);
        }

        private sealed class OverlapTrackingJsRuntime : IJSRuntime
        {
            private int _active;
            public int MaxConcurrentInitializations;
            public int InitializeCompleted;

            public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
                InvokeAsync<TValue>(identifier, CancellationToken.None, args);

            public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
            {
                if (identifier == "import")
                {
                    var module = new Mock<IJSObjectReference>();
                    module.Setup(m => m.InvokeAsync<bool>("initialize", It.IsAny<object?[]?>())).Returns(new ValueTask<bool>(true));
                    module.Setup(m => m.InvokeAsync<IJSObjectReference?>("initialize", It.IsAny<object?[]?>()))
                        .Returns(() => new ValueTask<IJSObjectReference?>(InitializeAnimationAsync()));
                    return new ValueTask<TValue>((TValue)module.Object);
                }
                throw new InvalidOperationException($"Unexpected call {identifier}");
            }

            private async Task<IJSObjectReference?> InitializeAnimationAsync()
            {
                var now = Interlocked.Increment(ref _active);
                int seen;
                while ((seen = MaxConcurrentInitializations) < now && Interlocked.CompareExchange(ref MaxConcurrentInitializations, now, seen) != seen) { }
                await Task.Delay(100);
                Interlocked.Decrement(ref _active);
                Interlocked.Increment(ref InitializeCompleted);
                var anim = new Mock<IJSObjectReference>();
                anim.Setup(a => a.InvokeAsync<IJSVoidResult>(It.IsAny<string>(), It.IsAny<object?[]?>())).Returns(new ValueTask<IJSVoidResult>(default(IJSVoidResult)!));
                anim.Setup(a => a.DisposeAsync()).Returns(ValueTask.CompletedTask);
                return anim.Object;
            }
        }
    }
}
