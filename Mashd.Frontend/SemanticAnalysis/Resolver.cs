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
    public SymbolTable GlobalScope { get; private set; }

    public Resolver(ErrorReporter errorReporter)
    {
        this._errorReporter = errorReporter;
        this.GlobalScope = new SymbolTable(errorReporter);
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
            GlobalScope.Add(function.Identifier, function);
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
        Console.WriteLine($"Visiting function definition: {node.Identifier}, it is in global scope: {GlobalScope.IsGlobalScope}");
        if (!GlobalScope.IsGlobalScope)
        {
            _errorReporter.Report.NameResolution(node, "Function cannot be declared inside a block.");
        }
        
        // Enter a new scope for the function
        node.Symbols = new SymbolTable(_errorReporter, GlobalScope);
        var outer = GlobalScope;
        GlobalScope = node.Symbols;

        // Register parameters
        foreach (var param in node.ParameterList.Parameters)
        {
            GlobalScope.Add(param.Identifier, param);
        }

        Resolve(node.Body);

        // Exit the function scope
        GlobalScope = outer;

        return DummyVoid.Null;
    }

    public DummyVoid VisitSchemaDefinitionNode(SchemaDefinitionNode node)
    {
        // Register the schema definition in the current scope
        GlobalScope.Add(node.Identifier, node);
    
        // Visit the schema object to handle any nested expressions or field references
        if (node.ObjectNode != null)
        {
            Resolve(node.ObjectNode);
        }
    
        return DummyVoid.Null;
    }

    public DummyVoid VisitDatasetDefinitionNode(DatasetDefinitionNode node)
    {
        // Register the dataset definition in the current scope
        GlobalScope.Add(node.Identifier, node);
    
        // Visit the dataset object to handle any nested expressions
        if (node.ObjectNode != null)
        {
            Resolve(node.ObjectNode);
        }
        
        if (node.MethodNode != null)
        {
            Resolve(node.MethodNode);
        }
    
        return DummyVoid.Null;
    }

    public DummyVoid VisitMashdDefinitionNode(MashdDefinitionNode node)
    {
        // Register the mashd definition in the current scope
        GlobalScope.Add(node.Identifier, node);
    
        // Visit left and right expressions to resolve any identifiers
        Resolve(node.Left);
        Resolve(node.Right);
    
        return DummyVoid.Null;
    }

    public DummyVoid VisitBlockNode(BlockNode node)
    {
        // Enter a new scope for the block
        node.Symbols = new SymbolTable(_errorReporter, GlobalScope);
        var outer = GlobalScope;
        GlobalScope = node.Symbols;

        Resolve(node.Statements);

        // Exit the block scope
        GlobalScope = GlobalScope.Parent;

        return DummyVoid.Null;
    }
    
    public DummyVoid VisitVariableDeclarationNode(VariableDeclarationNode node)
    {
        // Add the variable to the current scope
        GlobalScope.Add(node.Identifier, node);
        if (node.HasInitialization)
        {
            Resolve(node.Expression);
        }

        return DummyVoid.Null;
    }
    
    public DummyVoid VisitAssignmentNode(AssignmentNode node)
    {
        // Ensure target was declared
        if (!GlobalScope.TryLookup(node.Identifier, out var decl))
            _errorReporter.Report.NameResolution(node, $"Undefined symbol");
        node.Definition = decl;

        // Bind right‐hand side
        node.Expression.Accept(this);
        return DummyVoid.Null;
    }
    
    public DummyVoid VisitFunctionCallNode(FunctionCallNode node)
    {
        if (!GlobalScope.TryLookup(node.FunctionName, out var decl))
            _errorReporter.Report.NameResolution(node, $"Undefined function '{node.FunctionName}'");
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
        if (!GlobalScope.TryLookup(node.Name, out var decl))
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
        if (node.Left is not null)
            Resolve(node.Left);
        
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
        foreach (var pair in node.Pairs)
        {
            Resolve(pair.Value);
        }
        return DummyVoid.Null;
    }

    public DummyVoid VisitSchemaObjectNode(SchemaObjectNode objectNode)
    {
        // No need to resolve further for simple schema objects
        // If there were expressions in field values, we would resolve them here
        return DummyVoid.Null;
    }

    public DummyVoid VisitDatasetObjectNode(DatasetObjectNode node)
    {
        // Check if any property values need resolution (could be identifiers or expressions)
        foreach (var property in node.Properties.Values)
        {
            // If property contains expressions, resolve them
            // For example, if a property value could be an ExpressionNode:
            if (property.Value is ExpressionNode expr)
            {
                Resolve(expr);
            }
            else if (property.Value is string identifier && property.Key.ToLower() == "schema")
            {
                // Check if referenced schema exists in scope
                if (!GlobalScope.TryLookup(identifier, out var decl))
                {
                    _errorReporter.Report.NameResolution(node, $"Undefined schema '{identifier}'");
                }
                // Could store the resolved declaration if needed
            }
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