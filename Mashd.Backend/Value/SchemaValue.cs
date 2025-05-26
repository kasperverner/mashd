namespace Mashd.Backend.Value;

public class SchemaValue(Dictionary<string, SchemaFieldValue> raw) : IValue
{
    public readonly Dictionary<string, SchemaFieldValue> Raw = raw;

    public override string ToString()
    {
        return "{" + string.Join(", ", Raw.Select(kvp => $"{kvp.Key}: {kvp.Value.ToString()}")) + "}";
    }
}