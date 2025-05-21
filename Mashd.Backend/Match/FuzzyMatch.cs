using Mashd.Backend.Value;

namespace Mashd.Backend.Match;

public class FuzzyMatch(PropertyAccessValue left, PropertyAccessValue right, DecimalValue threshold) : IMatch
{
    public PropertyAccessValue Left { get; } = left;
    public PropertyAccessValue Right { get; } = right;
    public DecimalValue Threshold { get; } = threshold;
}