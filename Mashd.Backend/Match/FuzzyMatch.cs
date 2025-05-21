using Mashd.Backend.Value;

namespace Mashd.Backend.Match;

public class FuzzyMatch(SchemaFieldValue left, SchemaFieldValue right, DecimalValue threshold) : IMatch
{
    public SchemaFieldValue Left { get; } = left;
    public SchemaFieldValue Right { get; } = right;
    public DecimalValue Threshold { get; } = threshold;
}