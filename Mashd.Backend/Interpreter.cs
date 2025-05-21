using Mashd.Backend.Errors;
using Mashd.Frontend.AST;
using Mashd.Frontend.AST.Definitions;
using Mashd.Frontend.AST.Expressions;
using Mashd.Frontend.AST.Statements;
using Mashd.Frontend.SemanticAnalysis;
using System.Globalization;
using Mashd.Backend.BuiltInMethods;
using Mashd.Backend.Value;

namespace Mashd.Backend;

public class Interpreter : IAstVisitor<IValue>
{
    // Tolerance for floating-point comparisons
    private const double Tolerance = 1e-10;

    // Runtime store: map each declaration to its current Value
    private readonly Dictionary<IDeclaration, IValue> _values = new();

    // Expose values for testing or inspection
    public IReadOnlyDictionary<IDeclaration, IValue> Values => _values;

    // New: store function definitions
    private readonly Dictionary<FunctionDefinitionNode, FunctionDefinitionNode> _functions = new();

    // New: an activation record stack for parameters/locals
    private readonly Stack<Dictionary<IDeclaration, IValue>> _callStack = new();

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

        _callStack.Push(locals);

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
            _callStack.Pop();
        }
    }

    public IValue VisitVariableDeclarationNode(VariableDeclarationNode node)
    {
        var value = node switch
        {
            { InferredType: SymbolType.Dataset, Expression: ObjectExpressionNode objectNode } => HandleDatasetFromObjectNode(objectNode),
            { InferredType: SymbolType.Dataset, Expression: MethodChainExpressionNode methodChain } => HandleDatasetFromMethodNode(methodChain),
            { Expression: not null } => node.Expression.Accept(this),
            _ => new NullValue()
        };

        _values[node] = value;
        return value;
    }
    
    public IValue VisitAssignmentNode(AssignmentNode node)
    {
        var value = node switch
        {
            { InferredType: SymbolType.Dataset, Expression: ObjectExpressionNode objectNode } => HandleDatasetFromObjectNode(objectNode),
            { InferredType: SymbolType.Dataset, Expression: MethodChainExpressionNode methodChain } => HandleDatasetFromMethodNode(methodChain),
            { Expression: not null } => node.Expression.Accept(this),
            _ => new NullValue()
        };
        
        _values[node.Definition] = value;

        return value;
    }

    private DatasetValue HandleDatasetFromObjectNode(ObjectExpressionNode node)
    {
        var value = node.Accept(this);

        if (value is not ObjectValue objectValue)
            throw new ParseException("Invalid dataset object value.", node.Line, node.Column);

        var dataset = objectValue.ToDatasetValue();
        dataset.ValidateProperties();
        dataset.LoadData();
        dataset.ValidateData();

        return dataset;
    }

    private DatasetValue HandleDatasetFromMethodNode(MethodChainExpressionNode node)
    {
        var value = node.Accept(this);

        if (value is not DatasetValue datasetValue)
            throw new ParseException("Invalid dataset method chain value.", node.Line, node.Column);

        // TODO: Generate new schema
        // TODO: Generate new data based on method chain
        // TODO: Validate new data based on generated schema
        // TODO: Return new dataset value

        return datasetValue;
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
            return node.ElseBlock!.Accept(this);
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
            return CreateMashed(node);
        
        var leftVal = node.Left.Accept(this);
        var rightVal = node.Right.Accept(this);

        var value = node.Operator switch
        {
            // Arithmetic
            OpType.Add => EvaluateArithmetic(node.Operator, leftVal, rightVal, node),
            OpType.Subtract => EvaluateArithmetic(node.Operator, leftVal, rightVal, node),
            OpType.Multiply => EvaluateArithmetic(node.Operator, leftVal, rightVal, node),
            OpType.Divide => EvaluateArithmetic(node.Operator, leftVal, rightVal, node),
            OpType.Modulo => EvaluateArithmetic(node.Operator, leftVal, rightVal, node),

            // Comparison
            OpType.LessThan => EvaluateComparison(node.Operator, leftVal, rightVal),
            OpType.LessThanEqual => EvaluateComparison(node.Operator, leftVal, rightVal),
            OpType.GreaterThan => EvaluateComparison(node.Operator, leftVal, rightVal),
            OpType.GreaterThanEqual => EvaluateComparison(node.Operator, leftVal, rightVal),
            OpType.Equality => EvaluateComparison(node.Operator, leftVal, rightVal),
            OpType.Inequality => EvaluateComparison(node.Operator, leftVal, rightVal),

            // Logical
            OpType.LogicalAnd => new BooleanValue(
                ToBoolean(leftVal, node.Left) && ToBoolean(rightVal, node.Right)
            ),
            OpType.LogicalOr => new BooleanValue(
                ToBoolean(leftVal, node.Left) || ToBoolean(rightVal, node.Right)
            ),
            
            // Assignment
            OpType.NullishCoalescing => leftVal is NullValue ? rightVal : leftVal,
            
            _ => throw new NotImplementedException($"Binary operator {node.Operator} not implemented.")
        };

        return value;
    }

    private IValue CreateMashed(BinaryNode node)
    {
        var leftValue = node.Left.Accept(this);
        var rightValue = node.Right.Accept(this);

        if (leftValue is not DatasetValue ds1 || rightValue is not DatasetValue ds2)
            throw new NotImplementedException(
                $"Combine operator not implemented for types {leftValue.GetType()} and {rightValue.GetType()}.");

        if (node.Left is not IdentifierNode nodeLeft || node.Right is not IdentifierNode nodeRight)
            throw new NotImplementedException(
                $"Combine operator not implemented for types {node.Left.GetType()} and {node.Right.GetType()}.");

        return new MashdValue(nodeLeft.Name, ds1, nodeRight.Name, ds2);
    }

    public IValue VisitIdentifierNode(IdentifierNode node)
    {
        // 1) if inside a function, check the top of the call‐stack first
        if (_callStack.Count > 0 &&
            _callStack.Peek().TryGetValue(node.Definition, out var localVal))
        {
            return localVal;
        }

        // 2) otherwise fall back to global variables
        if (_values.TryGetValue(node.Definition, out var globalVal))
        {
            return globalVal;
        }
        
        // 3) check if the identifier is a function
        if (node.Definition is FunctionDefinitionNode def && _functions.TryGetValue(def, out var functionDef))
        {
            //TODO: Handle function as arguments
            return new NullValue();
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
        _functions[node] = node;
        return new NullValue();
    }

    public IValue VisitReturnNode(ReturnNode node)
    {
        var value = node.Expression.Accept(this);
        throw new FunctionReturnExceptionSignal(value);
    }

    public IValue VisitPropertyAccessExpressionNode(PropertyAccessExpressionNode node)
    {
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
            return InvokeStaticParse(tv, arguments, node);
        }

        var current = leftVal;
        var chain = node;
        
        while (chain != null)
        {
            var arguments = chain.Arguments
                .Select(a => a.Accept(this))
                .ToList();

            current = InvokeInstanceMethod(current, chain.MethodName, arguments);
            chain = chain.Next;
        }

        return current;
    }

    private IValue InvokeStaticParse(TypeValue tv, IValue argument, MethodChainExpressionNode node)
    {
        switch (tv.Raw)
        {
            case SymbolType.Integer:
                switch (argument)
                {
                    case TextValue t: return new IntegerValue(long.Parse(t.Raw));
                    case DecimalValue d: return new IntegerValue((long)d.Raw);
                    case IntegerValue i: return i;
                    default:
                        throw new Exception($"Integer.parse() cannot accept {argument.GetType().Name}");
                }

            case SymbolType.Decimal:
                switch (argument)
                {
                    case TextValue t: return new DecimalValue(double.Parse(t.Raw, CultureInfo.InvariantCulture));
                    case IntegerValue i: return new DecimalValue(i.Raw);
                    case DecimalValue d: return d;
                    default:
                        throw new Exception($"Decimal.parse() cannot accept {argument.GetType().Name}");
                }

            case SymbolType.Text:
                // Anything can become text via ToString()
                return new TextValue(argument.ToString() ?? string.Empty);
                

            case SymbolType.Boolean:
                switch (argument)
                {
                    case TextValue t: return new BooleanValue(bool.Parse(t.Raw));
                    case BooleanValue b: return b;
                    default:
                        throw new Exception($"Boolean.parse() cannot accept {argument.GetType().Name}");
                }

            case SymbolType.Date:
                if (argument is not TextValue dateString)
                    throw new Exception("Date.parse() requires the first argument to be a string");

                var parsed = node.Arguments.Count switch
                {
                    1 => Date.parse(dateString.Raw),
                    2 when node.Arguments[1].Accept(this) is TextValue formatString => Date.parse(dateString.Raw, formatString.Raw),
                    2 => throw new Exception("Date.parse() second argument must be a string format descriptor"),
                    _ => throw new Exception("Date.parse() requires 1 or 2 string arguments")
                };

                return new DateValue(parsed.Value);

            default:
                throw new Exception($"Type '{tv.Raw}' has no static parse()");
        }
    }

    private IValue InvokeInstanceMethod(IValue target, string methodName, List<IValue> arguments)
    {
        return target switch
        {
            DatasetValue ds when methodName == "toFile" => HandleToFile(ds, arguments),
            DatasetValue ds when methodName == "toTable" => HandleToTable(ds, arguments),
            
            MashdValue mv when methodName == "match" => HandleMatch(mv, arguments),
            MashdValue mv when methodName == "fuzzyMatch" => HandleFuzzyMatch(mv, arguments),
            MashdValue mv when methodName == "functionMatch" => HandleFunctionMatch(mv, arguments),
            MashdValue mv when methodName == "transform" => HandleTransform(mv, arguments),
            MashdValue mv when methodName == "join" => HandleJoin(mv, arguments),
            MashdValue mv when methodName == "union" => HandleUnion(mv, arguments),
            
            _ => throw new Exception("Invalid method call")
        };
    }

    private IValue HandleToFile(DatasetValue dataset, List<IValue> arguments)
    {
        if (arguments.Count != 1)
            throw new Exception("match() requires exactly one argument");

        var path = arguments.OfType<TextValue>().SingleOrDefault()?.Raw 
                   ?? throw new Exception("toFile requires a path string");
        
        dataset.ToFile(path);

        return dataset;
    }

    private IValue HandleToTable(DatasetValue dataset, List<IValue> arguments)
    {
        // TODO: Handle toTable with arguments
        
        dataset.ToTable();

        return dataset;
    }
    
    private IValue HandleMatch(MashdValue mashd, List<IValue> arguments)
    {
        if (arguments.Count != 2)
            throw new Exception("match() requires exactly two arguments");

        if (arguments[0] is not PropertyAccessValue left)
            throw new Exception("match() first argument must be a dataset property access");
        
        if (arguments[1] is not PropertyAccessValue right)
            throw new Exception("match() second argument must be a dataset property access");

        mashd.AddMatch(left, right);

        return mashd;
    }
        
    private IValue HandleFuzzyMatch(MashdValue mashd, List<IValue> arguments)
    {
        if (arguments.Count != 3)
            throw new Exception("fuzzyMatch() requires exactly three arguments");

        if (arguments[0] is not PropertyAccessValue left)
            throw new Exception("fuzzyMatch() first argument must be a dataset property access");
        
        if (arguments[1] is not PropertyAccessValue right)
            throw new Exception("fuzzyMatch() second argument must be a dataset property access");

        if (arguments[2] is not DecimalValue threshold)
            throw new Exception("fuzzyMatch() third argument must be a decimal value");
        
        mashd.AddMatch(left, right, threshold);

        return mashd;
    }
    
    private IValue HandleFunctionMatch(MashdValue mashd, List<IValue> arguments)
    {
        throw new NotImplementedException();
    }
    
    private IValue HandleTransform(MashdValue mashd, List<IValue> arguments)
    {
        if (arguments.Count != 1)
            throw new Exception("transform() requires exactly one arguments");

        if (arguments[0] is not ObjectValue objectValue)
            throw new Exception("transform() first argument must be an object");
        
        mashd.Transform(objectValue);
        
        return mashd;
    }
    
    private IValue HandleJoin(MashdValue mashd, List<IValue> arguments)
    {
        throw new NotImplementedException();
    }
    
    private IValue HandleUnion(MashdValue mashd, List<IValue> arguments)
    {
        throw new NotImplementedException();
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

    // Helper methods

    private IValue EvaluateArithmetic(OpType op, IValue leftVal, IValue rightVal, BinaryNode node)
    {
        switch (op)
        {
            case OpType.Add:
                if (leftVal is IntegerValue li && rightVal is IntegerValue ri)
                    return new IntegerValue(li.Raw + ri.Raw);
                if (leftVal is DecimalValue lf && rightVal is DecimalValue rf)
                    return new DecimalValue(lf.Raw + rf.Raw);
                if (leftVal is TextValue ls && rightVal is TextValue rs)
                    return new TextValue(ls.Raw + rs.Raw);
                break;
            case OpType.Subtract:
                if (leftVal is IntegerValue li2 && rightVal is IntegerValue ri2)
                    return new IntegerValue(li2.Raw - ri2.Raw);
                if (leftVal is DecimalValue lf2 && rightVal is DecimalValue rf2)
                    return new DecimalValue(lf2.Raw - rf2.Raw);
                break;
            case OpType.Multiply:
                if (leftVal is IntegerValue li3 && rightVal is IntegerValue ri3)
                    return new IntegerValue(li3.Raw * ri3.Raw);
                if (leftVal is DecimalValue lf3 && rightVal is DecimalValue rf3)
                    return new DecimalValue(lf3.Raw * rf3.Raw);
                break;
            case OpType.Divide:
                if (leftVal is IntegerValue li4 && rightVal is IntegerValue ri4)
                {
                    if (ri4.Raw == 0)
                    {
                        throw new DivisionByZeroException(node);
                    }

                    return new IntegerValue(li4.Raw / ri4.Raw);
                }

                if (leftVal is DecimalValue lf4 && rightVal is DecimalValue rf4)
                {
                    if (rf4.Raw == 0.0)
                    {
                        throw new DivisionByZeroException(node);
                    }

                    return new DecimalValue(lf4.Raw / rf4.Raw);
                }

                break;
            case OpType.Modulo:
                if (leftVal is IntegerValue li5 && rightVal is IntegerValue ri5)
                {
                    if (ri5.Raw == 0)
                    {
                        throw new DivisionByZeroException(node);
                    }

                    return new IntegerValue(li5.Raw % ri5.Raw);
                }

                break;
        }

        throw new NotImplementedException(
            $"Arithmetic {op} not implemented for types {leftVal.GetType()} and {rightVal.GetType()}.");
    }

    private IValue EvaluateComparison(OpType op, IValue leftVal, IValue rightVal)
    {
        // Numeric comparisons
        if (leftVal is IntegerValue li && rightVal is IntegerValue ri)
        {
            return new BooleanValue(
                op switch
                {
                    OpType.LessThan => li.Raw < ri.Raw,
                    OpType.LessThanEqual => li.Raw <= ri.Raw,
                    OpType.GreaterThan => li.Raw > ri.Raw,
                    OpType.GreaterThanEqual => li.Raw >= ri.Raw,
                    OpType.Equality => li.Raw == ri.Raw,
                    OpType.Inequality => li.Raw != ri.Raw,
                    _ => throw new InvalidOperationException()
                }
            );
        }

        if (leftVal is DecimalValue lf && rightVal is DecimalValue rf)
        {
            return new BooleanValue(
                op switch
                {
                    OpType.LessThan => lf.Raw < rf.Raw,
                    OpType.LessThanEqual => lf.Raw <= rf.Raw,
                    OpType.GreaterThan => lf.Raw > rf.Raw,
                    OpType.GreaterThanEqual => lf.Raw >= rf.Raw,
                    OpType.Equality => Math.Abs(lf.Raw - rf.Raw) < Tolerance,
                    OpType.Inequality => Math.Abs(lf.Raw - rf.Raw) > Tolerance,
                    _ => throw new InvalidOperationException()
                }
            );
        }

        // Text comparisons: only ==, !=
        if (leftVal is TextValue ls && rightVal is TextValue rs)
        {
            return new BooleanValue(
                op == OpType.Equality ? ls.Raw == rs.Raw : ls.Raw != rs.Raw
            );
        }

        // Boolean comparisons: only ==, !=
        if (leftVal is BooleanValue lb && rightVal is BooleanValue rb)
        {
            return new BooleanValue(
                op == OpType.Equality ? lb.Raw == rb.Raw : lb.Raw != rb.Raw
            );
        }

        throw new NotImplementedException(
            $"Comparison {op} not implemented for types {leftVal.GetType()} and {rightVal.GetType()}.");
    }

    private bool ToBoolean(IValue v, AstNode node)
    {
        if (v is BooleanValue bv) return bv.Raw;
        throw new TypeMismatchException(node);
    }

    // Main entry point for evaluation

    public IValue Evaluate(AstNode node)
    {
        return node.Accept(this);
    }
}