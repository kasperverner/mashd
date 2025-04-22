using System.Collections.Generic;
using Mashd.Frontend.AST.Definitions;
using Mashd.Frontend.AST.Statements;

namespace Mashd.Frontend.AST;


public class ProgramNode : AstNode
{
    public List<ImportNode> Imports { get; }
    public List<DefinitionNode> Definitions { get; }
    public List<StatementNode> Statements { get; }

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

}