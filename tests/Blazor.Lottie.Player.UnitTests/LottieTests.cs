using System.Reflection;
using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;

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
            var moduleMock = Context.JSInterop.SetupModule("./content/Blazor.Lottie.Player/loadLottieWeb.js");
            // Setup the initialize call to return true
            moduleMock.Setup<bool>("initialize", _ => true);

            var lottieModuleMock = Context.JSInterop.SetupModule("./content/Blazor.Lottie.Player/lottiePlayerModule.js");
            lottieModuleMock.Setup<bool>("initialize", _ => true).SetResult(true);

            lottieModuleMock.Setup<bool>("play", _ => true).SetResult(true);
            lottieModuleMock.Setup<bool>("pause", _ => true).SetResult(true);
            lottieModuleMock.Setup<bool>("stop", _ => true).SetResult(true);
            lottieModuleMock.Setup<bool>("destroy", _ => true).SetResult(true);
            lottieModuleMock.Setup<bool>("setSpeed", _ => true).SetResult(true);
            lottieModuleMock.Setup<bool>("setDirection", _ => true).SetResult(true);
        }

        [Test]
        public void LottieComponentRendersCorrectly()
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

            var prop = typeof(LottiePlayer).GetProperty("ElementRef", BindingFlags.NonPublic | BindingFlags.Instance);
            prop.Should().NotBeNull();
            var elementRef = (ElementReference)prop.GetValue(comp.Instance)!;
            elementRef.ShouldBeElementReferenceTo(element);

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

            comp.Instance.IsAnimationLoaded.Should().BeTrue();
            comp.Instance.IsAnimationPaused.Should().Be(!comp.Instance.AutoPlay);
            comp.Instance.CurrentAnimationFrame.Should().Be(0.0);
        }

        [Test]
        public void LottiePlayer_RendersUserAttributes()
        {
            var comp = Context.RenderComponent<LottiePlayer>(parameters => parameters
                .Add(p => p.Src, "https://example.com/lottie.json")
                .Add(p => p.UserAttributes, new Dictionary<string, object> { { "data-test", "abc" }, { "aria-label", "lottie" } })
            );
            var element = comp.Find($"#{comp.Instance.ElementId}");
            element.HasAttribute("data-test").Should().BeTrue();
            element.GetAttribute("data-test").Should().Be("abc");
            element.GetAttribute("aria-label").Should().Be("lottie");
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
        public void LottiePlayer_PublicMethods_ThrowIfModuleNull()
        {
            var comp = Context.RenderComponent<LottiePlayer>(parameters => parameters
                .Add(p => p.Src, "https://example.com/lottie.json")
            );
            // Simulate module disposal
            comp.Instance.GetType().GetField("_lottiePlayerModule", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(comp.Instance, null);

            Assert.ThrowsAsync<InvalidOperationException>(() => comp.Instance.PlayAnimationAsync());
            Assert.ThrowsAsync<InvalidOperationException>(() => comp.Instance.PauseAnimationAsync());
            Assert.ThrowsAsync<InvalidOperationException>(() => comp.Instance.StopAnimationAsync());
            Assert.ThrowsAsync<InvalidOperationException>(() => comp.Instance.SetSpeedAsync(1.0));
            Assert.ThrowsAsync<InvalidOperationException>(() => comp.Instance.SetDirectionAsync(LottieAnimationDirection.Forward));
        }

        [Test]
        public void LottiePlayer_ThrowsIfJsModuleFailsToLoad()
        {
            var moduleMock = Context.JSInterop.SetupModule("./content/Blazor.Lottie.Player/loadLottieWeb.js");
            moduleMock.Setup<bool>("initialize", _ => false);

            var ex = Assert.Throws<InvalidOperationException>(() =>
                Context.RenderComponent<LottiePlayer>(parameters => parameters
                    .Add(p => p.Src, "https://example.com/lottie.json")
                )
            );
            ex.Message.Should().Contain("Failed to initialize Lottie Web Player Module");
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
                .Add(p => p.UserAttributes, new Dictionary<string, object>
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

            var options = new LottieAnimationOptions();
            options.AnimationQuality = LottieAnimationQuality.Medium;
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

            var result = await comp.InvokeAsync<bool>(async () => await comp.Instance.SetSpeedAsync(2.0));
            result.Should().BeTrue();
            result = false;

            result = await comp.InvokeAsync<bool>(async () => await comp.Instance.SetDirectionAsync(LottieAnimationDirection.Reverse));
            result.Should().BeTrue();

            await comp.InvokeAsync(async () => await comp.Instance.StopAnimationAsync());
            comp.Instance.IsAnimationPaused.Should().BeTrue();
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
            domloadedMethod!.Invoke(comp.Instance, new object[] { this, new LottiePlayerLoadedEventArgs() });
            var animationReadyMethod = typeof(LottiePlayer).GetMethod("HandleAnimationReady", BindingFlags.NonPublic | BindingFlags.Instance);
            animationReadyMethod!.Invoke(comp.Instance, new object[] { this, new LottiePlayerLoadedEventArgs() });
            var animationCompletedMethod = typeof(LottiePlayer).GetMethod("HandleAnimationCompleted", BindingFlags.NonPublic | BindingFlags.Instance);
            animationCompletedMethod!.Invoke(comp.Instance, new object[] { this, true });
            var loopCompleteMethod = typeof(LottiePlayer).GetMethod("HandleAnimationLoopCompleted", BindingFlags.NonPublic | BindingFlags.Instance);
            loopCompleteMethod!.Invoke(comp.Instance, new object[] { this, true });
            var currentFrameChangedMethod = typeof(LottiePlayer).GetMethod("HandleCurrentFrameChanged", BindingFlags.NonPublic | BindingFlags.Instance);
            currentFrameChangedMethod!.Invoke(comp.Instance, new object[] { this, 17.0 });

            // Check event invocations
            handledom.Should().BeTrue();
            handleenterframe.Should().BeTrue();
            handlecomplete.Should().BeTrue();
            handleloopcomplete.Should().BeTrue();
            handleanimationready.Should().BeTrue();
        }

#pragma warning disable CS8619
        public static IEnumerable<object[]> CurrentFrameChangeFuncCases()
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
                .Add(p => p.CurrentFrameChanged, (Action<double>)(frame =>
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
                .Add(p => p.CurrentFrameChanged, (Action<double>)(_ => eventInvoked = true))
            );
            // Use reflection to invoke the private handler
            var method = typeof(LottiePlayer).GetMethod("HandleCurrentFrameChanged", BindingFlags.NonPublic | BindingFlags.Instance);
            method!.Invoke(comp.Instance, new object[] { null, 42.0 });
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
    }
}
