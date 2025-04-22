using Mashd.Frontend.TypeChecking;

namespace Mashd.Frontend.AST;

public abstract class ScopeNode : AstNode
{
    public SymbolTable Symbols { get; set; }
    
    protected ScopeNode(int line, int column, string text)
        : base(line, column, text)
    {
    }
}