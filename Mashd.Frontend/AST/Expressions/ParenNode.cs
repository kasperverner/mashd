﻿namespace Mashd.Frontend.AST.Expressions;

public class ParenNode : ExpressionNode
{
    public ExpressionNode InnerExpression { get; }

    public ParenNode(ExpressionNode innerExpression, int line, int column, string text, int level)
        : base(line, column, text, level)
    {
        InnerExpression = innerExpression;
    }

    public override T Accept<T>(IAstVisitor<T> visitor)
    {
        return visitor.VisitParenNode(this);
    }

    
}