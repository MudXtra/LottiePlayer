using AwesomeAssertions;
using Blazor.Lottie.Player.Extensions;

namespace Blazor.Lottie.Player.UnitTests;
public class IdentifierTests
{
    [Test]
    public void Identifier_Create_CanProduceEveryCharacterIncludingTheLast()
    {
        var seen = new HashSet<char>();
        for (var i = 0; i < 2000; i++)
        {
            foreach (var c in Identifier.Create("p-")[2..]) seen.Add(c);
        }
        seen.Should().Contain('9');
        seen.Count.Should().Be(36);
    }
}
