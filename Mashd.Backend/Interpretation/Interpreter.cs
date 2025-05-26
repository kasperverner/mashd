using System.Globalization;
using Mashd.Backend.Errors;
using Mashd.Backend.Value;
using Mashd.Frontend.AST;
using Mashd.Frontend.AST.Definitions;
using Mashd.Frontend.AST.Expressions;
using Mashd.Frontend.AST.Statements;
using Mashd.Frontend.SemanticAnalysis;

namespace Mashd.Backend.Interpretation;

public class Interpreter : IAstVisitor<IValue>
{
    private readonly CallStackHandler _callStackHandler = new();
    private readonly RowContextHandler _rowContextHandler = new();
    private readonly StorageHandler _storageHandler = new();
    private readonly FunctionHandler _functionHandler = new();
    private readonly DatasetHandler _datasetHandler = new();
    private readonly ExpressionHandler _expressionHandler = new();
    
    private readonly MethodChainHandler _methodChainHandler;
    
    public Interpreter()
    {
        _methodChainHandler = new MethodChainHandler(_datasetHandler, _callStackHandler, _rowContextHandler, this);
    }
    
    public IReadOnlyDictionary<IDeclaration, IValue> Values => _storageHandler.Values;

    public IValue VisitProgramNode(ProgramNode node)
    {
        // Phase 1: register every function
        foreach (var def in node.Definitions)
            if (def is FunctionDefinitionNode fn)
            {
                VisitFunctionDefinitionNode(fn);
            }

        // Phase 2: run everything (function definitions return null)
        IValue last = new NullValue();
        foreach (var stmt in node.Statements)
        {
            last = stmt.Accept(this);
        }

        return last;
    }

    public IValue VisitImportNode(ImportNode node)
    {
        return new NullValue();
    }

    public IValue VisitFunctionCallNode(FunctionCallNode node)
    {
        if (node.Definition is not FunctionDefinitionNode def)
            throw new InvalidOperationException("Function definition is missing or invalid.");

        // evaluate arguments
        var arguments = node.Arguments.Select(a => a.Accept(this)).ToArray();

        // bind parameters into a fresh activation‐record
        var locals = new Dictionary<IDeclaration, IValue>();
        for (int i = 0; i < def.ParameterList.Parameters.Count; i++)
        {
            IDeclaration paramDecl = def.ParameterList.Parameters[i];
            locals[paramDecl] = arguments[i];
        }

        _callStackHandler.Push(locals);

        try
        {
            // any return inside the body will throw FunctionReturnException
            foreach (var stmt in def.Body.Statements)
                stmt.Accept(this);
            // no return ⇒ default null (or you could choose 0/""/false)
            return null!;
        }
        catch (FunctionReturnExceptionSignal ret)
        {
            return ret.ReturnValue;
        }
        finally
        {
            _callStackHandler.Pop();
        }
    }

    public IValue VisitVariableDeclarationNode(VariableDeclarationNode node)
    {
        var value = node switch
        {
            { InferredType: SymbolType.Dataset, Expression: ObjectExpressionNode objectNode } => _datasetHandler.HandleDatasetFromObjectNode(this, objectNode),
            { InferredType: SymbolType.Dataset, Expression: MethodChainExpressionNode methodChain } => _datasetHandler.HandleDatasetFromMethodNode(this, methodChain),
            { Expression: not null } => node.Expression.Accept(this),
            _ => new NullValue()
        };

        _storageHandler.Set(node, value);
        return value;
    }
    
    public IValue VisitAssignmentNode(AssignmentNode node)
    {
        var value = node switch
        {
            { InferredType: SymbolType.Dataset, Expression: ObjectExpressionNode objectNode } => _datasetHandler.HandleDatasetFromObjectNode(this, objectNode),
            { InferredType: SymbolType.Dataset, Expression: MethodChainExpressionNode methodChain } => _datasetHandler.HandleDatasetFromMethodNode(this, methodChain),
            _ => node.Expression.Accept(this)
        };
        
        _storageHandler.Set(node.Definition, value);
        return value;
    }

    
    
    public IValue VisitIfNode(IfNode node)
    {
        var conditionValue = node.Condition.Accept(this);
        if (conditionValue is not BooleanValue booleanValue)
            return new NullValue();

        if (booleanValue.Raw)
        {
            return node.ThenBlock.Accept(this);
        }

        if (node.HasElse && !booleanValue.Raw)
        {
            return node.ElseBlock.Accept(this);
        }

        return new NullValue();
    }

    public IValue VisitTernaryNode(TernaryNode node)
    {
        var conditionValue = node.Condition.Accept(this);
        if (conditionValue is BooleanValue booleanValue)
        {
            return booleanValue.Raw ? node.TrueExpression.Accept(this) : node.FalseExpression.Accept(this);
        }

        throw new TypeMismatchException(node.Condition);
    }

    public IValue VisitExpressionStatementNode(ExpressionStatementNode node)
    {
        node.Expression.Accept(this);
            
        return new NullValue();
    }

    public IValue VisitParenNode(ParenNode node)
    {
        return node.InnerExpression.Accept(this);
    }

    public IValue VisitLiteralNode(LiteralNode node)
    {
        if (node.Value is null)
            return new NullValue();

        return node.InferredType switch
        {
            SymbolType.Integer => new IntegerValue((long)node.Value),
            SymbolType.Decimal => new DecimalValue((double)node.Value),
            SymbolType.Text => new TextValue((string)node.Value),
            SymbolType.Boolean => new BooleanValue((bool)node.Value),
            _ => throw new NotImplementedException($"Literal type {node.InferredType} not implemented.")
        };

    }

    public IValue VisitTypeLiteralNode(TypeLiteralNode node)
    {
        return new TypeValue(node.InferredType);
    }

    public IValue VisitUnaryNode(UnaryNode node)
    {
        var value = node.Operand.Accept(this);
        return node.Operator switch
        {
            OpType.Negation => value switch
            {
                IntegerValue iv => new IntegerValue(-iv.Raw),
                DecimalValue dv => new DecimalValue(-dv.Raw),
                _ => throw new NotImplementedException($"Unary negation not implemented for type {value.GetType()}.")
            },
            OpType.Not => value switch
            {
                BooleanValue bv => new BooleanValue(!bv.Raw),
                _ => throw new NotImplementedException($"Unary not operator not implemented for type {value.GetType()}.")
            },
            _ => throw new NotImplementedException($"Unary operator {node.Operator} not implemented.")
        };
    }

    public IValue VisitBinaryNode(BinaryNode node)
    {
        if (node.Operator == OpType.Combine)
            return MashdHandler.CreateMashed(this, node);
        
        var leftVal = node.Left.Accept(this);
        var rightVal = node.Right.Accept(this);

        var value = node.Operator switch
        {
            // Arithmetic
            OpType.Add => _expressionHandler.EvaluateArithmetic(node.Operator, leftVal, rightVal, node),
            OpType.Subtract => _expressionHandler.EvaluateArithmetic(node.Operator, leftVal, rightVal, node),
            OpType.Multiply => _expressionHandler.EvaluateArithmetic(node.Operator, leftVal, rightVal, node),
            OpType.Divide => _expressionHandler.EvaluateArithmetic(node.Operator, leftVal, rightVal, node),
            OpType.Modulo => _expressionHandler.EvaluateArithmetic(node.Operator, leftVal, rightVal, node),

            // Comparison
            OpType.LessThan => _expressionHandler.EvaluateComparison(node.Operator, leftVal, rightVal),
            OpType.LessThanEqual => _expressionHandler.EvaluateComparison(node.Operator, leftVal, rightVal),
            OpType.GreaterThan => _expressionHandler.EvaluateComparison(node.Operator, leftVal, rightVal),
            OpType.GreaterThanEqual => _expressionHandler.EvaluateComparison(node.Operator, leftVal, rightVal),
            OpType.Equality => _expressionHandler.EvaluateComparison(node.Operator, leftVal, rightVal),
            OpType.Inequality => _expressionHandler.EvaluateComparison(node.Operator, leftVal, rightVal),

            // Logical
            OpType.LogicalAnd => new BooleanValue(
                _methodChainHandler.ToBoolean(leftVal, node.Left) && _methodChainHandler.ToBoolean(rightVal, node.Right)
            ),
            OpType.LogicalOr => new BooleanValue(
                _methodChainHandler.ToBoolean(leftVal, node.Left) || _methodChainHandler.ToBoolean(rightVal, node.Right)
            ),
            
            // Assignment
            OpType.NullishCoalescing => _expressionHandler.EvaluateNullishCoalescing(leftVal, rightVal),
            _ => throw new NotImplementedException($"Binary operator {node.Operator} not implemented.")
        };

        return value;
    }

    

    public IValue VisitIdentifierNode(IdentifierNode node)
    {
        if (_rowContextHandler.Count > 0)
        {
            var context = _rowContextHandler.Peek();
            if (node.Name == context.LeftIdentifier || node.Name == context.RightIdentifier)
            {
                return new DatasetPlaceholderValue(node.Name);
            }
        }
    
        if (_callStackHandler.Count > 0 &&
            _callStackHandler.TryGetValue(node.Definition, out var localVal))
        {
            return localVal;
        }

        if (_storageHandler.TryGet(node.Definition, out var globalVal))
        {
            return globalVal;
        }
    
        if (node.Definition is FunctionDefinitionNode def && _functionHandler.TryGetFunction(def, out var functionDef))
        {
            return new FunctionDefinitionValue(functionDef);
        }

        throw new UndefinedVariableException(node);
    }

    public IValue VisitBlockNode(BlockNode node)
    {
        IValue last = new NullValue();
        foreach (var stmt in node.Statements)
        {
            last = stmt.Accept(this);
        }

        return last;
    }

    public IValue VisitFormalParameterNode(FormalParameterNode node)
    {
        return new NullValue();
    }

    public IValue VisitFormalParameterListNode(FormalParameterListNode node)
    {
        return new NullValue();
    }

    public IValue VisitFunctionDefinitionNode(FunctionDefinitionNode node)
    {
        // register the function AST-node so calls can find it
        _functionHandler.Register(node);
        return new NullValue();
    }

    public IValue VisitReturnNode(ReturnNode node)
    {
        var value = node.Expression.Accept(this);
        throw new FunctionReturnExceptionSignal(value);
    }

    public IValue VisitPropertyAccessExpressionNode(PropertyAccessExpressionNode node)
    {
        if (_rowContextHandler.Count > 0)
        {
            return _methodChainHandler.HandlePropertyAccessInRowContext(node);
        }
        
        if (node.Left is not IdentifierNode identifier)
            throw new ParseException("Property access only valid on identifiers.", node.Line, node.Column);
        
        var value = node.Left.Accept(this);
        
        if (value is not DatasetValue datasetValue)
        {
            throw new ParseException("Property access only valid on datasets.", node.Line, node.Column);
        }
        
        if (!datasetValue.Schema.Raw.TryGetValue(node.Property, out var fieldValue))
        {
            throw new ParseException($"Property '{node.Property}' not found in schema.", node.Line, node.Column);
        }
        
        return new PropertyAccessValue(fieldValue, identifier.Name, node.Property);
    }
    
    

    

    public IValue VisitMethodChainExpressionNode(MethodChainExpressionNode node)
    {
        var leftVal = node.Left.Accept(this);

        if (leftVal is TypeValue tv && node is { MethodName: "parse", Next: null })
        {
            if (node.Arguments.Count is < 1 or > 2)
            {
                throw new Exception("parse() requires exactly one ore two arguments");
            }

            var arguments = node.Arguments[0].Accept(this);
            return _methodChainHandler.InvokeStaticParse(tv, arguments, node);
        }

        var current = leftVal;
        var chain = node;
        
        while (chain != null)
        {
            current = _methodChainHandler.InvokeInstanceMethod(current, chain.MethodName, chain.Arguments);
            chain = chain.Next;
        }

        return current;
    }

    public IValue VisitDateLiteralNode(DateLiteralNode node)
    {
        if (DateTime.TryParseExact(node.Text, "yyyy-MM-dd", null, DateTimeStyles.None, out var parsedDate))
        {
            return new DateValue(parsedDate);
        }

        throw new FormatException($"Invalid date format: {node.Text}. Expected ISO 8601 (yyyy-MM-dd).");
    }

    public IValue VisitObjectExpressionNode(ObjectExpressionNode node)
    {
        var properties = new Dictionary<string, IValue>();
        foreach (var (key, expression) in node.Properties)
        {
            var value = expression.Accept(this);

            properties[key] = value;
        }

        return new ObjectValue(properties);
    }

    // Main entry point for evaluation
    public IValue Evaluate(AstNode node)
    {
        return node.Accept(this);
    }
}