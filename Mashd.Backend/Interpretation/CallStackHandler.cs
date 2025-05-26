using System.Diagnostics.CodeAnalysis;
using Mashd.Backend.Value;
using Mashd.Frontend.SemanticAnalysis;

namespace Mashd.Backend.Interpretation;

public class CallStackHandler
{
    private readonly Stack<Dictionary<IDeclaration, IValue>> _stack = new();
    
    public void Push(Dictionary<IDeclaration, IValue> frame) => _stack.Push(frame);
    public void Pop() => _stack.Pop();
    public bool TryGetValue(IDeclaration declaration, [NotNullWhen(true)] out IValue? value)
    {
        value = null;
        return _stack.Count > 0 && _stack.Peek().TryGetValue(declaration, out value);
    }
    public int Count => _stack.Count;
}