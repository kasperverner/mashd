namespace Mashd.Frontend.AST.Expressions;

public class FunctionCallNode : ExpressionNode
{
    public string FunctionName { get; }
    
    List<ExpressionNode> Arguments { get; }
    
    public FunctionCallNode(string functionName, List<ExpressionNode> arguments, int line, int column, string text)
        : base(line, column, text)
    {
        FunctionName = functionName;
        Arguments = arguments;
    }
    
    public override T Accept<T>(IAstVisitor<T> visitor)
    {
        return visitor.VisitFunctionCallNode(this);
    }
}