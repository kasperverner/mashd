namespace Mashd.Frontend.AST;

using Mashd.Frontend.AST.Expressions;
using Mashd.Frontend.AST.Statements;
using Mashd.Frontend.AST.Definitions;
public interface IAstVisitor<T>
{
    T VisitProgramNode(ProgramNode node);
    T VisitImportNode(ImportNode node);
    T VisitVariableDefinitionNode(VariableDefinitionNode node);
    T VisitParenNode(ParenNode node);
    T VisitLiteralNode(LiteralNode node);
    T VisitUnaryNode(UnaryNode node);
    T VisitBinaryNode(BinaryNode node);
    T VisitIdentifierNode(IdentifierNode node);
    
    T VisitVariableDeclarationNode(VariableDeclarationNode node);
    T VisitAssignmentNode(AssignmentNode node);
    T VisitCompoundAssignmentNode(CompoundAssignmentNode node);

}