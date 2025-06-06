﻿using Mashd.Frontend.SemanticAnalysis;

namespace Mashd.Frontend.AST;

public abstract class ScopeNode : AstNode
{
    public SymbolTable Symbols { get; set; }
    
    protected ScopeNode(int line, int column, string text, int level)
        : base(line, column, text, level)
    {
    }
}