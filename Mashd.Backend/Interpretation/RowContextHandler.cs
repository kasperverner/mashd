using System.Diagnostics.CodeAnalysis;
using Mashd.Backend.Value;
using Mashd.Frontend.SemanticAnalysis;

namespace Mashd.Backend.Interpretation;

public class RowContextHandler
{
    private readonly Stack<RowContext> _stack = new();
    public void Push(RowContext context) => _stack.Push(context);
    public RowContext Peek() => _stack.Peek();
    public void Pop() => _stack.Pop();
    public int Count => _stack.Count;
}