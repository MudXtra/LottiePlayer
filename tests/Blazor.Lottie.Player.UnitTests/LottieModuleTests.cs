using AwesomeAssertions;
using Bunit;
using Moq;

namespace Blazor.Lottie.Player.UnitTests;
public class LottieModuleTests : BunitTest
{
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
    public void LottiePlayerModule_Initialization()
    {
        var comp = Context.RenderComponent<LottiePlayer>(parameters => parameters
            .Add(p => p.Src, "https://example.com/lottie.json")
        );

        var lottieModule = comp.Instance._lottiePlayerModule;
        lottieModule.Should().NotBeNull();

        lottieModule._isInitialized.Should().BeTrue();

        // after init module, jsruntime, elementref, and dotnetobjectref should be setup
        lottieModule._module.Should().NotBeNull();
        lottieModule._js.Should().NotBeNull();
        lottieModule._elementRef.Should().NotBeNull();
        lottieModule._dotNetReference.Should().NotBeNull();
        lottieModule._isDisposing.Should().BeFalse();

        // verify element ref
        lottieModule._elementRef.Should().NotBeNull();
    }

    [Test]
    public void LottiePlayerModule_Events()
    {
        var comp = Context.RenderComponent<LottiePlayer>(parameters => parameters
            .Add(p => p.Src, "https://example.com/lottie.json")
        );

        var lottieModule = comp.Instance._lottiePlayerModule;
        lottieModule.Should().NotBeNull();

        var loadedEventArgs = new LottiePlayerLoadedEventArgs
        {
            ElementId = comp.Instance.ElementId,
            CurrentFrame = 0.0,
            TotalFrames = 100.0,
        };

        var eventTriggered = false;
        lottieModule.OnDOMLoaded += (sender, args) => eventTriggered = true;
        lottieModule.DOMLoadedEvent(loadedEventArgs);
        eventTriggered.Should().BeTrue();

        eventTriggered = false;
        lottieModule.OnAnimationReady += (sender, args) => eventTriggered = true;
        lottieModule.AnimationReadyEvent(loadedEventArgs);
        eventTriggered.Should().BeTrue();

        eventTriggered = false;
        lottieModule.OnEnterFrame += (sender, args) => eventTriggered = true;
        lottieModule.EnterFrameEvent(5.7);
        eventTriggered.Should().BeTrue();

        eventTriggered = false;
        lottieModule.OnComplete += (sender, args) => eventTriggered = true;
        lottieModule.CompleteEvent();
        eventTriggered.Should().BeTrue();

        eventTriggered = false;
        lottieModule.OnLoopComplete += (sender, args) => eventTriggered = true;
        lottieModule.LoopCompleteEvent();
        eventTriggered.Should().BeTrue();
    }

    [Test]
    public void LottiePlayerModule_Actions()
    {
        var comp = Context.RenderComponent<LottiePlayer>(parameters => parameters
            .Add(p => p.Src, "https://example.com/lottie.json")
        );

        var lottieModule = comp.Instance._lottiePlayerModule;
        lottieModule.Should().NotBeNull();

        // Test play
        lottieModule.PlayAsync().GetAwaiter().GetResult().Should().BeTrue();
        Context.JSInterop.VerifyInvoke("play");

        // Test pause
        lottieModule.PauseAsync().GetAwaiter().GetResult().Should().BeTrue();
        Context.JSInterop.VerifyInvoke("pause");

        // Test stop
        lottieModule.StopAsync().GetAwaiter().GetResult().Should().BeTrue();
        Context.JSInterop.VerifyInvoke("stop");

        // Test set speed
        lottieModule.SetSpeedAsync(1.5).GetAwaiter().GetResult().Should().BeTrue();
        Context.JSInterop.VerifyInvoke("setSpeed");

        // Test set direction
        lottieModule.SetDirectionAsync(LottieAnimationDirection.Forward).GetAwaiter().GetResult().Should().BeTrue();
        Context.JSInterop.VerifyInvoke("setDirection");
    }

    [Test]
    public void LottiePlayerModule_Dispose()
    {
        var comp = Context.RenderComponent<LottiePlayer>(parameters => parameters
            .Add(p => p.Src, "https://example.com/lottie.json")
        );
        var lottieModule = comp.Instance._lottiePlayerModule;
        lottieModule.Should().NotBeNull();
        // Dispose the module
        lottieModule.DisposeAsync().GetAwaiter().GetResult();
        lottieModule._isDisposing.Should().BeTrue();
        lottieModule._module.Should().BeNull();

        // test CanExecute returns false after dispose
        lottieModule.CanExecute.Should().BeFalse();
        var eventTriggered = false;

        // test actions can't execute after dispose
        lottieModule.PlayAsync().GetAwaiter().GetResult().Should().BeFalse();
        lottieModule.PauseAsync().GetAwaiter().GetResult().Should().BeFalse();
        lottieModule.StopAsync().GetAwaiter().GetResult().Should().BeFalse();
        lottieModule.SetSpeedAsync(1.5).GetAwaiter().GetResult().Should().BeFalse();
        lottieModule.SetDirectionAsync(LottieAnimationDirection.Forward).GetAwaiter().GetResult().Should().BeFalse();

        // test events can't trigger after dispose
        lottieModule.OnDOMLoaded += (sender, args) => eventTriggered = true;
        lottieModule.DOMLoadedEvent(It.IsAny<LottiePlayerLoadedEventArgs>());
        eventTriggered.Should().BeFalse();
        lottieModule.OnAnimationReady += (sender, args) => eventTriggered = true;
        lottieModule.AnimationReadyEvent(It.IsAny<LottiePlayerLoadedEventArgs>());
        eventTriggered.Should().BeFalse();
        lottieModule.OnEnterFrame += (sender, args) => eventTriggered = true;
        lottieModule.EnterFrameEvent(7.2);
        eventTriggered.Should().BeFalse();
        lottieModule.OnComplete += (sender, args) => eventTriggered = true;
        lottieModule.CompleteEvent();
        eventTriggered.Should().BeFalse();
        lottieModule.OnLoopComplete += (sender, args) => eventTriggered = true;
        lottieModule.LoopCompleteEvent();
        eventTriggered.Should().BeFalse();

        Context.JSInterop.VerifyInvoke("destroy");
    }
}
