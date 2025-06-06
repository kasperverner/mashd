﻿using Mashd.Frontend.SemanticAnalysis;

namespace Mashd.Frontend.AST.Statements;

using Mashd.Frontend.AST.Expressions;

public class AssignmentNode : StatementNode
{
    public string Identifier { get; }
    public ExpressionNode Expression { get; }
    
    public IDeclaration Definition {get; set; }
    
    public AssignmentNode(string identifier, ExpressionNode expression, int line, int column, string text, int level)
        : base(line, column, text, level)
    {
        Identifier = identifier;
        Expression = expression;
    }
    
    public override T Accept<T>(IAstVisitor<T> visitor)
    {
        return visitor.VisitAssignmentNode(this);
    }
}