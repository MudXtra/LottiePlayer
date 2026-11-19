using AwesomeAssertions;
using Bunit;
using Microsoft.JSInterop;

namespace Blazor.Lottie.Player.UnitTests;
public class SourceIntegrityTests : BunitTest
{
    [Test]
    public void Options_Default_UsesCdnSourceWithItsIntegrity()
    {
        var o = new LottieAnimationOptions();
        o.LottieSourceJs.Should().Be(LottieAnimationOptions.DefaultLottieSourceJs);
        o.LottieSourceJsIntegrity.Should().Be("sha512-uOtp2vx2X/5+tLBEf5UoQyqwAkFZJBM5XwGa7BfXDnWR+wdpRvlSVzaIVcRe3tGNsStu6UMDCeXKEnr4IBT8gA==");
    }

    [Test]
    public void Options_CustomSourceWithoutIntegrity_HasNoIntegrity()
    {
        new LottieAnimationOptions { LottieSourceJs = "https://example.com/lottie.js" }
            .LottieSourceJsIntegrity.Should().BeNull();
    }

    [Test]
    public void Options_ExplicitIntegrity_IsKeptAndEmptyDisablesIt()
    {
        new LottieAnimationOptions { LottieSourceJs = "https://example.com/lottie.js", LottieSourceJsIntegrity = "sha384-x" }
            .LottieSourceJsIntegrity.Should().Be("sha384-x");
        new LottieAnimationOptions { LottieSourceJsIntegrity = "" }.LottieSourceJsIntegrity.Should().BeEmpty();
    }

    [Test]
    public void Options_EqualsAndHashCode_IncludeIntegrity()
    {
        var a = new LottieAnimationOptions();
        var b = new LottieAnimationOptions { LottieSourceJsIntegrity = "sha384-x" };
        a.Equals(b).Should().BeFalse();
        a.Equals(new LottieAnimationOptions()).Should().BeTrue();
        a.GetHashCode().Should().Be(new LottieAnimationOptions().GetHashCode());
    }

    [Test]
    public void Component_PassesSourceAndIntegrityToLoader_LocalFallbackGetsNone()
    {
        var moduleMock = Context.JSInterop.SetupModule("./_content/Blazor.Lottie.Player/loadLottieWeb.js");
        moduleMock.Setup<bool>("initialize", _ => true).SetResult(false);

        Context.RenderComponent<LottiePlayer>(p => p.Add(x => x.Src, "https://example.com/a.json"));

        var calls = moduleMock.Invocations["initialize"].ToList();
        calls.Should().HaveCount(2, "configured source, then the local fallback once");
        calls[0].Arguments.Should().Equal(LottieAnimationOptions.DefaultLottieSourceJs, LottieAnimationOptions.DefaultLottieSourceJsIntegrity);
        calls[1].Arguments.Should().ContainSingle().Which.Should().Be("./_content/Blazor.Lottie.Player/lottie.min.js");
    }
}
