namespace Blazor.Lottie.Player.Extensions;
internal class Identifier
{
    private const string _chars = "abcdefghijklmnopqrstuvwxyz0123456789";
    private const int _charsLength = 35;
    private const int _randomStringLength = 8;

    public static string Create(ReadOnlySpan<char> prefix)
    {
        Span<char> identifierSpan = stackalloc char[prefix.Length + _randomStringLength];
        prefix.CopyTo(identifierSpan);
        for (var i = 0; i < _randomStringLength; i++)
        {
            var index = Random.Shared.Next(_charsLength);
            identifierSpan[prefix.Length + i] = _chars[index];
        }
        return identifierSpan.ToString();
    }

    public static string Create() => Create("mudxtra-");
}
