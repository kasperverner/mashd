using System.Diagnostics.CodeAnalysis;
using Mashd.Backend.Value;
using Mashd.Frontend.SemanticAnalysis;

namespace Mashd.Backend.Interpretation;

public class StorageHandler
{
    private readonly Dictionary<IDeclaration, IValue> _values = new();
    
    public void Set(IDeclaration declaration, IValue value) => _values[declaration] = value;
    public bool TryGet(IDeclaration declaration, [NotNullWhen(true)] out IValue? value) => _values.TryGetValue(declaration, out value);
    public IReadOnlyDictionary<IDeclaration, IValue> Values => _values;
}