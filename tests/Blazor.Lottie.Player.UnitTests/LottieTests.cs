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

            var field = typeof(LottiePlayer).GetField("ElementRef", BindingFlags.NonPublic | BindingFlags.Instance);
            field.Should().NotBeNull();
            var elementRef = (ElementReference)field.GetValue(comp.Instance)!;
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
            comp.Instance.IsAnimationPaused.Should().Be(comp.Instance.AutoPlay);
            comp.Instance.CurrentAnimationFrame.Should().Be(0.0);
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

    }
}
