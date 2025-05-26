using Mashd.Backend.Value;

namespace Mashd.Backend.Match;

public class FuzzyMatchCondition(PropertyAccessValue left, PropertyAccessValue right, DecimalValue threshold) : ICondition
{
    public PropertyAccessValue Left { get; } = left;
    public PropertyAccessValue Right { get; } = right;
    public DecimalValue Threshold { get; } = threshold;
}