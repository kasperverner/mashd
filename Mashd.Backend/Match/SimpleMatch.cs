using Mashd.Backend.Value;

namespace Mashd.Backend.Match;

public class SimpleMatch(TextValue left, TextValue right) : IMatch
{
    public TextValue Left { get; } = left;
    public TextValue Right { get; } = right;
}