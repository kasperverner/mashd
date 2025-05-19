namespace Mashd.Frontend.AST.Expressions
{
    public class MethodChainExpressionNode : ExpressionNode
    {
        public ExpressionNode Left { get; }
        public string MethodName { get; }
        public List<ExpressionNode> Arguments { get; }
        public MethodChainExpressionNode? Next { get; }

        public MethodChainExpressionNode(
            ExpressionNode left,
            string methodName,
            List<ExpressionNode> arguments,
            MethodChainExpressionNode? next,
            int line,
            int column,
            string text,
            int level
        ) : base(line, column, text, level)
        {
            Left = left;
            MethodName = methodName;
            Arguments = arguments;
            Next = next;
        }

        public override T Accept<T>(IAstVisitor<T> visitor)
        {
            return visitor.VisitMethodChainExpressionNode(this);
        }
    }
}