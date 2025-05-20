namespace Mashd.Frontend.AST.Definitions;

public abstract class DefinitionNode : ScopeNode
{
    protected DefinitionNode(int line, int column, string text, int level)
        : base(line, column, text, level)
    {
    }
    
}