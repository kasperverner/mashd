using Mashd.Backend.Value;

namespace Mashd.Backend.Match;

public class SimpleMatch(PropertyAccessValue left, PropertyAccessValue right) : IMatch
{
    public PropertyAccessValue Left { get; } = left;
    public PropertyAccessValue Right { get; } = right;
}