using Mashd.Frontend.AST;
using Mashd.Frontend.AST.Definitions;
using Mashd.Frontend.AST.Expressions;
using Mashd.Frontend.AST.Statements;

namespace Mashd.Frontend.SemanticAnalysis;

public struct DummyVoid
{
    public static readonly DummyVoid Null = new DummyVoid();
}

public class Resolver : IAstVisitor<DummyVoid>
{
    private readonly ErrorReporter _errorReporter;
    private SymbolTable currentScope; // global scope

    public Resolver(ErrorReporter errorReporter)
    {
        this._errorReporter = errorReporter;
        this.currentScope = new SymbolTable(errorReporter);
    }
    public DummyVoid VisitProgramNode(ProgramNode node)
    {
        var nodes = new List<AstNode>()
            .Concat(node.Definitions)
            .Concat(node.Statements)
            .OrderBy(x => x.Level)
            .ThenBy(x => x.Line)
            .ToList();
           
        // process each in source order
        foreach (var item in nodes)
        {
            switch (item)
            {
                case FunctionDefinitionNode fn:
                    // 1) register function name
                    currentScope.Add(fn.Identifier, fn);
                    // 2) resolve its body in a nested scope
                    Resolve(fn);
                    break;

                case VariableDeclarationNode vd:
                    // 1) register variable name
                    currentScope.Add(vd.Identifier, vd);
                    // 2) resolve initializer expression if any
                    Resolve(vd);
                    break;

                default:
                    // a non‐declaration statement
                    Resolve(item);
                    break;
            }
        }
        
        return DummyVoid.Null;
    }

    public DummyVoid VisitImportNode(ImportNode node)
    {
        return DummyVoid.Null;
    }
    
    public DummyVoid VisitFunctionDefinitionNode(FunctionDefinitionNode node)
    {
        if (!currentScope.IsGlobalScope)
        {
            _errorReporter.Report.NameResolution(node, "Function cannot be declared inside a block.");
        }
        
        // Enter a new scope for the function
        node.Symbols = new SymbolTable(_errorReporter, currentScope);
        var outer = currentScope;
        currentScope = node.Symbols;

        // Register parameters
        foreach (var param in node.ParameterList.Parameters)
        {
            currentScope.Add(param.Identifier, param);
        }

        Resolve(node.Body);

        // Exit the function scope
        currentScope = outer;

        return DummyVoid.Null;
    }

    public DummyVoid VisitBlockNode(BlockNode node)
    {
        // Enter a new scope for the block
        node.Symbols = new SymbolTable(_errorReporter, currentScope);
        var outer = currentScope;
        currentScope = node.Symbols;

        Resolve(node.Statements);

        // Exit the block scope
        currentScope = currentScope.Parent;

        return DummyVoid.Null;
    }
    
    public DummyVoid VisitVariableDeclarationNode(VariableDeclarationNode node)
    {
        // Add the variable to the current scope, if it wasn't already registered at the top-level
        if (!currentScope.IsGlobalScope)
        {
            currentScope.Add(node.Identifier, node);
        }
        
        if (node.Expression != null)
        {
            Resolve(node.Expression);
        }

        return DummyVoid.Null;
    }
    
    public DummyVoid VisitAssignmentNode(AssignmentNode node)
    {
        // Ensure target was declared
        if (!currentScope.TryLookup(node.Identifier, out var decl))
            _errorReporter.Report.NameResolution(node, $"Undefined symbol");
        node.Definition = decl;

        // Bind right‐hand side
        node.Expression.Accept(this);
        return DummyVoid.Null;
    }
    
    public DummyVoid VisitFunctionCallNode(FunctionCallNode node)
    {
        if (!currentScope.TryLookup(node.FunctionName, out var decl))
        {
            _errorReporter.Report.NameResolution(node, $"Undefined function '{node.FunctionName}'");
        }
        node.Definition = decl;
        
        foreach (var arg in node.Arguments)
        {
            Resolve(arg);
        }
        
        return DummyVoid.Null;
    }

    public DummyVoid VisitIdentifierNode(IdentifierNode node)
    {
        // Check if the identifier is defined in the current scope
        if (!currentScope.TryLookup(node.Name, out var decl))
            _errorReporter.Report.NameResolution(node, $"Undefined symbol '{node.Name}'");
        node.Definition = decl;

        return DummyVoid.Null;
    }
    
    public DummyVoid VisitParenNode(ParenNode node)
    {
        Resolve(node.InnerExpression);
        
        return DummyVoid.Null;
    }

    public DummyVoid VisitLiteralNode(LiteralNode node)
    {
        return DummyVoid.Null;
    }

    public DummyVoid VisitTypeLiteralNode(TypeLiteralNode node)
    {
        return DummyVoid.Null;
    }

    public DummyVoid VisitUnaryNode(UnaryNode node)
    {
        Resolve(node.Operand);
        
        return DummyVoid.Null;
    }

    public DummyVoid VisitBinaryNode(BinaryNode node)
    {
        Resolve(node.Left);
        Resolve(node.Right);
        
        return DummyVoid.Null;
    }


    public DummyVoid VisitFormalParameterListNode(FormalParameterListNode node)
    {
        return DummyVoid.Null;
    }

    public DummyVoid VisitFormalParameterNode(FormalParameterNode node)
    {
        return DummyVoid.Null;
    }

    public DummyVoid VisitIfNode(IfNode node)
    {
        Resolve(node.Condition);
        Resolve(node.ThenBlock);
        if (node.HasElse)
        {
            Resolve(node.ElseBlock);
        }
        
        return DummyVoid.Null;
    }

    public DummyVoid VisitTernaryNode(TernaryNode node)
    {
        Resolve(node.Condition);
        Resolve(node.TrueExpression);
        Resolve(node.FalseExpression);
        
        return DummyVoid.Null;
    }

    public DummyVoid VisitExpressionStatementNode(ExpressionStatementNode node)
    {
        Resolve(node.Expression);
        
        return DummyVoid.Null;
    }

    public DummyVoid VisitReturnNode(ReturnNode node)
    {
        Resolve(node.Expression);
        return DummyVoid.Null;
    }

    public DummyVoid VisitPropertyAccessExpressionNode(PropertyAccessExpressionNode node)
    {
        Resolve(node.Left);
        
        return DummyVoid.Null;
    }

    public DummyVoid VisitMethodChainExpressionNode(MethodChainExpressionNode node)
    {
        if (node.Left is not null)
        {
            Resolve(node.Left);
        }
        
        foreach (var method in node.Arguments)
        {
            Resolve(method);
        }
        
        return DummyVoid.Null;
    }

    public DummyVoid VisitDateLiteralNode(DateLiteralNode node)
    {
        return DummyVoid.Null;
    }

    public DummyVoid VisitObjectExpressionNode(ObjectExpressionNode node)
    {
        foreach (var pair in node.Properties)
        {
            Resolve(pair.Value);
        }
        return DummyVoid.Null;
    }

    // Helper methods
    void Resolve(List<StatementNode> nodes)
    {
        foreach (var statement in nodes)
        {
            Resolve(statement);
        }
    }

    public void Resolve(AstNode node)
    {
        node.Accept(this);
    }
}