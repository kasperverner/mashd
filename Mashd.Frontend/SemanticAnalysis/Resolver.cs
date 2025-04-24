using Mashd.Frontend.AST;
using Mashd.Frontend.AST.Definitions;
using Mashd.Frontend.AST.Statements;
using Mashd.Frontend.AST.Expressions;

namespace Mashd.Frontend.TypeChecking;

public struct DummyVoid
{
    public static readonly DummyVoid Null = new DummyVoid();
}

public class Resolver : IAstVisitor<DummyVoid>
{
    private readonly ErrorReporter errorReporter;
    private SymbolTable currentScope; // global scope

    public Resolver(ErrorReporter errorReporter)
    {
        this.errorReporter = errorReporter;
        this.currentScope = new SymbolTable(errorReporter);
    }
    public DummyVoid VisitProgramNode(ProgramNode node)
    {
        // Register all imports, TBD
        foreach (var import in node.Imports)
        {
            Resolve(import);
        }

        // Register all top-level definitions, aka functions
        foreach (var function in node.Definitions.OfType<FunctionDefinitionNode>())
        {
            currentScope.Add(function.Identifier, function);
        }

        // Bind each function to its definition
        foreach (var definition in node.Definitions)
        {
            Resolve(definition);
        }

        // Register all top-level statements
        Resolve(node.Statements);

        return DummyVoid.Null;
    }

    public DummyVoid VisitImportNode(ImportNode node)
    {
        throw new NotImplementedException();
    }
    
    public DummyVoid VisitFunctionDefinitionNode(FunctionDefinitionNode node)
    {
        Console.WriteLine($"Visiting function definition: {node.Identifier}, it is in global scope: {currentScope.IsGlobalScope}");
        if (!currentScope.IsGlobalScope)
        {
            errorReporter.Report.NameResolution(node, "Function cannot be declared inside a block.");
        }
        
        // Enter a new scope for the function
        node.Symbols = new SymbolTable(errorReporter, currentScope);
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

    public DummyVoid VisitSchemaDefinitionNode(SchemaDefinitionNode node)
    {
        throw new NotImplementedException();
    }

    public DummyVoid VisitDatasetDefinitionNode(DatasetDefinitionNode node)
    {
        throw new NotImplementedException();
    }

    public DummyVoid VisitMashdDefinitionNode(MashdDefinitionNode node)
    {
        throw new NotImplementedException();
    }

    public DummyVoid VisitBlockNode(BlockNode node)
    {
        // Enter a new scope for the block
        node.Symbols = new SymbolTable(errorReporter, currentScope);
        var outer = currentScope;
        currentScope = node.Symbols;

        Resolve(node.Statements);

        // Exit the block scope
        currentScope = currentScope.Parent;

        return DummyVoid.Null;
    }
    
    public DummyVoid VisitVariableDeclarationNode(VariableDeclarationNode node)
    {
        // Add the variable to the current scope
        currentScope.Add(node.Identifier, node);
        if (node.HasInitialization)
        {
            Resolve(node.Expression);
        }

        return DummyVoid.Null;
    }
    
    public DummyVoid VisitAssignmentNode(AssignmentNode node)
    {
        // Ensure target was declared
        if (!currentScope.TryLookup(node.Identifier, out var decl))
            errorReporter.Report.NameResolution(node, $"Undefined symbol");
        node.Definition = decl;

        // Bind right‐hand side
        node.Expression.Accept(this);
        return DummyVoid.Null;
    }
    
    public DummyVoid VisitFunctionCallNode(FunctionCallNode node)
    {
        if (!currentScope.TryLookup(node.FunctionName, out var decl))
            errorReporter.Report.NameResolution(node, $"Undefined function '{node.FunctionName}'");
        node.Definition = decl;
        
        foreach (var arg in node.Arguments)
        {
            Resolve(arg);
        }
        
        return DummyVoid.Null;
    }



    public DummyVoid VisitCompoundAssignmentNode(CompoundAssignmentNode node)
    {
        throw new NotImplementedException();
    }

    public DummyVoid VisitIdentifierNode(IdentifierNode node)
    {
        // Check if the identifier is defined in the current scope
        if (!currentScope.TryLookup(node.Name, out var decl))
            errorReporter.Report.NameResolution(node, $"Undefined symbol '{node.Name}'");
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

    public DummyVoid VisitReturnNode(ReturnNode node)
    {
        Resolve(node.Expression);
        return DummyVoid.Null;
    }

    public DummyVoid VisitDatasetCombineExpressionNode(MashdDefinitionNode node)
    {
        Resolve(node.Left);
        Resolve(node.Right);
        
        return DummyVoid.Null;
    }

    public DummyVoid VisitPropertyAccessExpressionNode(PropertyAccessExpressionNode node)
    {
        Resolve(node.Left);
        
        return DummyVoid.Null;
    }

    public DummyVoid VisitMethodChainExpressionNode(MethodChainExpressionNode node)
    {
        throw new NotImplementedException();
    }

    public DummyVoid VisitDateLiteralNode(DateLiteralNode node)
    {
        return DummyVoid.Null;
    }

    public DummyVoid VisitObjectExpressionNode(ObjectExpressionNode node)
    {
        foreach (var pair in node.Pairs)
        {
            Resolve(pair.Value);
        }
        return DummyVoid.Null;
    }

    public DummyVoid VisitSchemaObjectNode(SchemaObjectNode objectNode)
    {
        throw new NotImplementedException();
    }

    public DummyVoid VisitDatasetObjectNode(DatasetObjectNode node)
    {
        throw new NotImplementedException();
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