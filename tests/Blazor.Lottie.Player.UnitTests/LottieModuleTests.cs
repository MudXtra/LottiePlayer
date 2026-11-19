using AwesomeAssertions;
using Bunit;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;
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

    private static (LottiePlayerModule Module, Mock<IJSObjectReference> Ref) CreateModuleWithMockedRef()
    {
        var js = new Mock<IJSRuntime>();
        var animationRef = new Mock<IJSObjectReference>();
        animationRef
            .Setup(r => r.InvokeAsync<IJSVoidResult>(It.IsAny<string>(), It.IsAny<object?[]?>()))
            .Returns(new ValueTask<IJSVoidResult>(default(IJSVoidResult)!));
        animationRef.Setup(r => r.DisposeAsync()).Returns(ValueTask.CompletedTask);
        var module = new LottiePlayerModule(js.Object, default) { _lottieAnimationRef = animationRef.Object };
        return (module, animationRef);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task LottiePlayerModule_GoToAndPlay_SendsFrameFlag(bool force)
    {
        var (module, animationRef) = CreateModuleWithMockedRef();
        await module.GoToAndPlay(12, force);
        animationRef.Verify(r => r.InvokeAsync<IJSVoidResult>("goToAndPlay",
            It.Is<object?[]?>(a => a!.Length == 2 && (double)a[0]! == 12d && (bool)a[1]! == true)), Times.Once);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task LottiePlayerModule_GoToAndStop_SendsFrameFlag(bool force)
    {
        var (module, animationRef) = CreateModuleWithMockedRef();
        await module.GoToAndStop(12, force);
        animationRef.Verify(r => r.InvokeAsync<IJSVoidResult>("goToAndStop",
            It.Is<object?[]?>(a => a!.Length == 2 && (double)a[0]! == 12d && (bool)a[1]! == true)), Times.Once);
    }

    private static IEnumerable<Func<Exception>> DisposeExceptions()
    {
        yield return () => new JSDisconnectedException("circuit gone");
        yield return () => new TaskCanceledException();
        yield return () => new OperationCanceledException();
        yield return () => new ObjectDisposedException("ref");
        yield return () => new JSException("already destroyed");
    }

    [TestCaseSource(nameof(DisposeExceptions))]
    public async Task LottiePlayerModule_Dispose_ToleratesDestroyFailure(Func<Exception> makeException)
    {
        var (module, animationRef) = CreateModuleWithMockedRef();
        animationRef
            .Setup(r => r.InvokeAsync<IJSVoidResult>("destroy", It.IsAny<object?[]?>()))
            .Returns(new ValueTask<IJSVoidResult>(Task.FromException<IJSVoidResult>(makeException())));
        var dotNetRef = module._dotNetReference;

        await module.Invoking(m => m.DisposeAsync().AsTask()).Should().NotThrowAsync();

        module._lottieAnimationRef.Should().BeNull();
        module._dotNetReference.Should().BeNull();
        Assert.Throws<ObjectDisposedException>(() => _ = dotNetRef!.Value);
    }

    [TestCaseSource(nameof(DisposeExceptions))]
    public async Task LottiePlayerModule_Dispose_ToleratesRefDisposeFailure(Func<Exception> makeException)
    {
        var (module, animationRef) = CreateModuleWithMockedRef();
        animationRef.Setup(r => r.DisposeAsync()).Returns(new ValueTask(Task.FromException(makeException())));
        var dotNetRef = module._dotNetReference;

        await module.Invoking(m => m.DisposeAsync().AsTask()).Should().NotThrowAsync();

        module._lottieAnimationRef.Should().BeNull();
        module._dotNetReference.Should().BeNull();
        Assert.Throws<ObjectDisposedException>(() => _ = dotNetRef!.Value);
    }

    [TestCase(0d)]
    [TestCase(-1d)]
    public async Task LottiePlayerModule_SetSpeed_RejectsNonPositiveSpeed(double speed)
    {
        var (module, animationRef) = CreateModuleWithMockedRef();
        await module.SetSpeedAsync(speed);
        animationRef.Verify(r => r.InvokeAsync<IJSVoidResult>("setSpeed", It.IsAny<object?[]?>()), Times.Never);

        await module.SetSpeedAsync(0.5);
        animationRef.Verify(r => r.InvokeAsync<IJSVoidResult>("setSpeed",
            It.Is<object?[]?>(a => a!.Length == 1 && (double)a[0]! == 0.5d)), Times.Once);
    }
}
