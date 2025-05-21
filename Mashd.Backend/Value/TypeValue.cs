using Mashd.Frontend.AST;

namespace Mashd.Backend.Value;

public class TypeValue(SymbolType raw) : IValue
{
    public SymbolType Raw { get; } = raw;
    public override string ToString() => $"<type:{Raw}>";
}