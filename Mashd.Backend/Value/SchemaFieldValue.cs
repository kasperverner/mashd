using Mashd.Frontend.AST;

namespace Mashd.Backend.Value;

public class SchemaFieldValue(SymbolType type, string name) : IValue
{
    public readonly SymbolType Type = type;
    public readonly string Name = name;

    public override string ToString()
    {
        return $"{{ Type: {Type.ToString()}, Name: {Name} }}";
    }
}