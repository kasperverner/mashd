namespace Mashd.Backend.Value;

public class ObjectValue(Dictionary<string, IValue> raw) : IValue
{
    public readonly Dictionary<string, IValue> Raw = raw;
    public override string ToString()
    {
        return "{" + string.Join(", ", Raw.Select(x => $"{x.Key}: {x.Value}")) + "}";
    }
}