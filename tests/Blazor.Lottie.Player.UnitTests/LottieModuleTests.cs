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
        var moduleMock = Context.JSInterop.SetupModule("./_content/Blazor.Lottie.Player/loadLottieWeb.js");
        // Setup the initialize call to return true
        moduleMock.Setup<bool>("initialize", _ => true).SetResult(true);

        var lottieModuleMock = Context.JSInterop.SetupModule("./_content/Blazor.Lottie.Player/lottiePlayerModule.js");
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

        var lottieModule = comp.Instance.LottiePlayerModule;
        lottieModule.Should().NotBeNull();

        lottieModule._lottieAnimationRef.Should().NotBeNull();

        // after init, jsruntime, elementref, and dotnetobjectref should be setup
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

        var lottieModule = comp.Instance.LottiePlayerModule;
        lottieModule.Should().NotBeNull();

        var loadedEventArgs = new LottiePlayerLoadedEventArgs
        {
            ElementId = comp.Instance.ElementId,
            CurrentFrame = 0.0,
            TotalFrames = 100.0,
        };

        var eventTriggered = false;
        lottieModule.OnDOMLoaded += (sender, args) => eventTriggered = true;
        comp.InvokeAsync(() => lottieModule.DOMLoadedEvent(loadedEventArgs));
        eventTriggered.Should().BeTrue();

        eventTriggered = false;
        lottieModule.OnAnimationReady += (sender, args) => eventTriggered = true;
        comp.InvokeAsync(() => lottieModule.AnimationReadyEvent(loadedEventArgs));
        eventTriggered.Should().BeTrue();

        eventTriggered = false;
        lottieModule.OnEnterFrame += (sender, args) => eventTriggered = true;
        comp.InvokeAsync(() => lottieModule.EnterFrameEvent(new LottiePlayerEventFrameArgs() { CurrentTime = 23.7 }));
        eventTriggered.Should().BeTrue();

        eventTriggered = false;
        lottieModule.OnComplete += (sender, args) => eventTriggered = true;
        comp.InvokeAsync(() => lottieModule.CompleteEvent());
        eventTriggered.Should().BeTrue();

        eventTriggered = false;
        lottieModule.OnLoopComplete += (sender, args) => eventTriggered = true;
        comp.InvokeAsync(() => lottieModule.LoopCompleteEvent());
        eventTriggered.Should().BeTrue();
    }

    [Test]
    public async Task LottiePlayerModule_Actions()
    {
        var comp = Context.RenderComponent<LottiePlayer>(parameters => parameters
            .Add(p => p.Src, "https://example.com/lottie.json")
            .Add(p => p.AutoPlay, false)
        );

        var lottieModule = comp.Instance.LottiePlayerModule;
        lottieModule.Should().NotBeNull();

        // Test play
        await lottieModule.PlayAsync();
        Context.JSInterop.VerifyInvoke("play");

        // Test pause
        await lottieModule.PauseAsync();
        Context.JSInterop.VerifyInvoke("pause");

        // Test stop
        await lottieModule.StopAsync();
        Context.JSInterop.VerifyInvoke("stop");

        // Test set speed
        await lottieModule.SetSpeedAsync(1.5);
        Context.JSInterop.VerifyInvoke("setSpeed");

        // Test set direction
        await lottieModule.SetDirectionAsync(LottieAnimationDirection.Forward);
        Context.JSInterop.VerifyInvoke("setDirection");
    }

    [Test]
    public async Task LottiePlayerModule_Dispose()
    {
        var comp = Context.RenderComponent<LottiePlayer>(parameters => parameters
            .Add(p => p.Src, "https://example.com/lottie.json")
            .Add(p => p.AutoPlay, false)
        );
        var lottieModule = comp.Instance.LottiePlayerModule;
        lottieModule.Should().NotBeNull();
        // Dispose the module
        await lottieModule.DisposeAsync();
        lottieModule._isDisposing.Should().BeTrue();
        lottieModule._lottieAnimationRef.Should().BeNull();

        // test CanExecute returns false after dispose
        lottieModule.CanExecute.Should().BeFalse();
        var eventTriggered = false;

        // test actions can't execute after dispose
        await lottieModule.PlayAsync();
        Context.JSInterop.VerifyNotInvoke("play");
        await lottieModule.PauseAsync();
        Context.JSInterop.VerifyNotInvoke("pause");
        await lottieModule.StopAsync();
        Context.JSInterop.VerifyNotInvoke("stop");
        await lottieModule.SetSpeedAsync(1.5);
        Context.JSInterop.VerifyNotInvoke("setSpeed");
        await lottieModule.SetDirectionAsync(LottieAnimationDirection.Forward);
        Context.JSInterop.VerifyNotInvoke("setDirection");

        // test events can't trigger after dispose
        lottieModule.OnDOMLoaded += (sender, args) => eventTriggered = true;
        await lottieModule.DOMLoadedEvent(It.IsAny<LottiePlayerLoadedEventArgs>());
        eventTriggered.Should().BeFalse();
        lottieModule.OnAnimationReady += (sender, args) => eventTriggered = true;
        await lottieModule.AnimationReadyEvent(It.IsAny<LottiePlayerLoadedEventArgs>());
        eventTriggered.Should().BeFalse();
        lottieModule.OnEnterFrame += (sender, args) => eventTriggered = true;
        await lottieModule.EnterFrameEvent(new LottiePlayerEventFrameArgs());
        eventTriggered.Should().BeFalse();
        lottieModule.OnComplete += (sender, args) => eventTriggered = true;
        await lottieModule.CompleteEvent();
        eventTriggered.Should().BeFalse();
        lottieModule.OnLoopComplete += (sender, args) => eventTriggered = true;
        await lottieModule.LoopCompleteEvent();
        eventTriggered.Should().BeFalse();

        Context.JSInterop.VerifyInvoke("destroy");
    }
}
