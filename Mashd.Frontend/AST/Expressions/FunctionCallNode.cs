using Mashd.Frontend.SemanticAnalysis;

namespace Mashd.Frontend.AST.Expressions;

public class FunctionCallNode : ExpressionNode
{
    public string FunctionName { get; }
    
    public List<ExpressionNode> Arguments { get; }
    
    public IDeclaration Definition {get; set; }

    
    public FunctionCallNode(string functionName, List<ExpressionNode> arguments, int line, int column, string text, int level)
        : base(line, column, text, level)
    {
        FunctionName = functionName;
        Arguments = arguments;
    }
    
    public override T Accept<T>(IAstVisitor<T> visitor)
    {
        return visitor.VisitFunctionCallNode(this);
    }
}