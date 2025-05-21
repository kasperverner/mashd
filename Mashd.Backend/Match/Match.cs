using Mashd.Backend.Value;

namespace Mashd.Backend.Match;

public class Match(SchemaFieldValue left, SchemaFieldValue right) : IMatch
{
    public SchemaFieldValue Left { get; } = left;
    public SchemaFieldValue Right { get; } = right;
}