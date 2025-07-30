using Bunit;

namespace Blazor.Lottie.Player.UnitTests;
public abstract class BunitTest
{
    protected Bunit.TestContext Context { get; private set; } = null!;

    [SetUp]
    public virtual void Setup()
    {
        Context = new Bunit.TestContext();
        //Context.Services.; // This is where you add services if needed
        Context.JSInterop.Mode = JSRuntimeMode.Loose; // Use Loose mode for JS Interop
    }

    [TearDown]
    public virtual void TearDown()
    {
        Context.Dispose();
        Context = null!;
    }
}
