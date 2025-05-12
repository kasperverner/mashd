using System.Collections.Generic;
using Mashd.Frontend.AST.Definitions;
using Mashd.Frontend.AST.Statements;

namespace Mashd.Frontend.AST;


public class ProgramNode : AstNode
{
    public List<ImportNode> Imports { get; private set; }
    public List<DefinitionNode> Definitions { get; private set; }
    public List<StatementNode> Statements { get; private set; }

    public ProgramNode(List<ImportNode> imports, List<DefinitionNode> definitions, List<StatementNode> statements, int line, int column, string text)
        : base(line, column, text)
    {
        Imports = imports;
        Definitions = definitions;
        Statements = statements;
    }
    
    public override T Accept<T>(IAstVisitor<T> visitor)
    {
        return visitor.VisitProgramNode(this);
    }
    
    public void Merge(ProgramNode other)
    {
        Imports.InsertRange(0, other.Imports);
        Definitions.InsertRange(0, other.Definitions);
        Statements.InsertRange(0, other.Statements);
    }
}