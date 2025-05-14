using Mashd.Frontend.AST.Expressions;
using Mashd.Frontend.AST.Statements;
using Mashd.Frontend.AST.Definitions;

namespace Mashd.Frontend.AST;

public interface IAstVisitor<T>
{
    T VisitProgramNode(ProgramNode node);
    T VisitImportNode(ImportNode node);
    T VisitBlockNode(BlockNode node);
    T VisitFormalParameterNode(FormalParameterNode node);
    T VisitFormalParameterListNode(FormalParameterListNode node);
    
    // Definitions
    T VisitFunctionDefinitionNode(FunctionDefinitionNode node);
    
    // Expressions
    T VisitParenNode(ParenNode node);
    T VisitLiteralNode(LiteralNode node);
    T VisitTypeLiteralNode(TypeLiteralNode node);
    T VisitUnaryNode(UnaryNode node);
    T VisitBinaryNode(BinaryNode node);
    T VisitIdentifierNode(IdentifierNode node);
    
    T VisitFunctionCallNode(FunctionCallNode node);
    
    // Statements
    T VisitVariableDeclarationNode(VariableDeclarationNode node);
    T VisitAssignmentNode(AssignmentNode node);
    
    T VisitIfNode(IfNode node);
    
    T VisitTernaryNode(TernaryNode node);
    
    T VisitExpressionStatementNode(ExpressionStatementNode node);
    
    T VisitReturnNode(ReturnNode node);
    
    T VisitPropertyAccessExpressionNode(PropertyAccessExpressionNode node);
    
    T VisitMethodChainExpressionNode(MethodChainExpressionNode node);
    
    T VisitDateLiteralNode(DateLiteralNode node);
    
    T VisitObjectExpressionNode(ObjectExpressionNode node);
}