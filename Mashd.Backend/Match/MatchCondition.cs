using Mashd.Backend.Value;

namespace Mashd.Backend.Match;

public class MatchCondition(PropertyAccessValue left, PropertyAccessValue right) : ICondition
{
    public PropertyAccessValue Left { get; } = left;
    public PropertyAccessValue Right { get; } = right;
}