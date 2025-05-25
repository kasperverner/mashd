using Mashd.Backend.Errors;
using Mashd.Frontend.AST;
using Mashd.Frontend.AST.Definitions;
using Mashd.Frontend.AST.Expressions;
using Mashd.Frontend.AST.Statements;
using Mashd.Frontend.SemanticAnalysis;
using System.Globalization;
using Mashd.Backend.BuiltInMethods;
using Mashd.Backend.Match;
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
    
    private readonly Stack<RowContext> _rowContextStack = new();

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
            _ => node.Expression.Accept(this)
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
        if (_rowContextStack.Count > 0)
        {
            var context = _rowContextStack.Peek();
            if (node.Name == context.LeftIdentifier || node.Name == context.RightIdentifier)
            {
                return new DatasetPlaceholderValue(node.Name);
            }
        }
    
        if (_callStack.Count > 0 &&
            _callStack.Peek().TryGetValue(node.Definition, out var localVal))
        {
            return localVal;
        }

        if (_values.TryGetValue(node.Definition, out var globalVal))
        {
            return globalVal;
        }
    
        if (node.Definition is FunctionDefinitionNode def && _functions.TryGetValue(def, out var functionDef))
        {
            return new FunctionDefinitionValue(def);
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
        if (_rowContextStack.Count > 0)
        {
            return HandlePropertyAccessInRowContext(node);
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
    
    private IValue HandlePropertyAccessInRowContext(PropertyAccessExpressionNode node)
    {
        if (node.Left is not IdentifierNode identifier)
            throw new Exception("Property access requires an identifier in row context");
        
        var context = _rowContextStack.Peek();
        Dictionary<string, object> row;
        
        if (identifier.Name == context.LeftIdentifier)
        {
            row = context.LeftRow;
        }
        else if (identifier.Name == context.RightIdentifier)
        {
            row = context.RightRow;
        }
        else
        {
            throw new Exception($"Unknown dataset identifier: {identifier.Name}");
        }

        if (identifier.Definition is not VariableDeclarationNode variable)
            throw new Exception("Property access requires a variable declaration node.");

        if (variable.Accept(this) is not DatasetValue datasetValue)
            throw new Exception("Property access only valid on dataset variables.");
        
        var schema = datasetValue.Schema.Raw[node.Property];
        
        if (!row.TryGetValue(schema.Name, out var rawValue))
        {
            return new NullValue();
        }
        
        return ConvertRawToIValue(rawValue);
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
            current = InvokeInstanceMethod(current, chain.MethodName, chain.Arguments);
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

    private IValue InvokeInstanceMethod(IValue target, string methodName, List<ExpressionNode> arguments)
    {
        return target switch
        {
            DatasetValue ds when methodName == "toFile" => HandleToFile(ds, arguments),
            DatasetValue ds when methodName == "toTable" => HandleToTable(ds, arguments),
            
            MashdValue mv when methodName == "match" => HandleMatch(mv, arguments),
            MashdValue mv when methodName == "fuzzyMatch" => HandleFuzzyMatch(mv, arguments),
            MashdValue mv when methodName == "functionMatch" => HandleFunctionMatch(mv, arguments),
            MashdValue mv when methodName == "transform" => HandleTransform(mv, arguments),
            MashdValue mv when methodName == "join" => HandleJoin(mv),
            MashdValue mv when methodName == "union" => HandleUnion(mv),
            
            _ => throw new Exception("Invalid method call")
        };
    }

    private IValue HandleToFile(DatasetValue dataset, List<ExpressionNode> arguments)
    {
        if (arguments.Count != 1)
            throw new Exception("match() requires exactly one argument");

        var path = arguments
                       .Select(x => x.Accept(this))
                       .OfType<TextValue>()
                       .SingleOrDefault()?.Raw
                   ?? throw new Exception("toFile requires a path string");
        
        dataset.ToFile(path);

        return dataset;
    }

    private IValue HandleToTable(DatasetValue dataset, List<ExpressionNode> arguments)
    {
        dataset.ToTable();

        return dataset;
    }
    
    private IValue HandleMatch(MashdValue mashd, List<ExpressionNode> arguments)
    {
        if (arguments.Count != 2)
            throw new Exception("match() requires exactly two arguments");

        var left = arguments[0].Accept(this);
        var right = arguments[1].Accept(this);
        
        if (left is not PropertyAccessValue leftProp)
            throw new Exception("match() first argument must be a dataset property access");
        
        if (right is not PropertyAccessValue rightProp)
            throw new Exception("match() second argument must be a dataset property access");

        mashd.AddCondition(leftProp, rightProp);

        return mashd;
    }
        
    private IValue HandleFuzzyMatch(MashdValue mashd, List<ExpressionNode> arguments)
    {
        if (arguments.Count != 3)
            throw new Exception("fuzzyMatch() requires exactly three arguments");

        var left = arguments[0].Accept(this);
        var right = arguments[1].Accept(this);
        
        if (left is not PropertyAccessValue leftProp)
            throw new Exception("fuzzyMatch() first argument must be a dataset property access");
        
        if (right is not PropertyAccessValue rightProp)
            throw new Exception("fuzzyMatch() second argument must be a dataset property access");

        var threshold = arguments[2].Accept(this);
        
        if (threshold is not DecimalValue thresholdValue)
            throw new Exception("fuzzyMatch() third argument must be a decimal value");
        
        mashd.AddCondition(leftProp, rightProp, thresholdValue);

        return mashd;
    }
    
    private IValue HandleFunctionMatch(MashdValue mashd, List<ExpressionNode> arguments)
    {
        var functionDefinition = arguments[0].Accept(this) as FunctionDefinitionValue 
            ?? throw new Exception("functionMatch() first argument must be a function definition");
        
        var actualParameters = arguments
            .Skip(1)
            .Select(x => x.Accept(this))
            .ToArray();
        
        ValidateFunctionMatchArguments(functionDefinition.Node, actualParameters);
        
        functionDefinition.AddArgument(actualParameters);
        mashd.AddCondition(functionDefinition);
        
        return mashd;
    }
    
    private void ValidateFunctionMatchArguments(FunctionDefinitionNode function, IValue[] arguments)
    {
        if (function.ParameterList.Parameters.Count != arguments.Length)
            throw new Exception($"Function {function.Identifier} requires {function.ParameterList.Parameters.Count} arguments, but got {arguments.Length}.");
        
        for (int i = 0; i < function.ParameterList.Parameters.Count; i++)
        {
            var parameter = function.ParameterList.Parameters[i];
            var argument = arguments[i];
            
            switch (argument)
            {
                case PropertyAccessValue propertyAccess when parameter.DeclaredType != propertyAccess.FieldValue.Type:
                    throw new Exception($"Argument {i + 1} for function {function.Identifier} does not match expected type {parameter.DeclaredType}.");
                case TextValue when parameter.DeclaredType != SymbolType.Text:
                    throw new Exception($"Argument {i + 1} for function {function.Identifier} must be a text value.");
                case IntegerValue when parameter.DeclaredType != SymbolType.Integer:
                    throw new Exception($"Argument {i + 1} for function {function.Identifier} must be an integer value.");
                case DecimalValue when parameter.DeclaredType != SymbolType.Decimal:
                    throw new Exception($"Argument {i + 1} for function {function.Identifier} must be a decimal value.");
                case BooleanValue when parameter.DeclaredType != SymbolType.Boolean:
                    throw new Exception($"Argument {i + 1} for function {function.Identifier} must be a boolean value.");
                case DateValue when parameter.DeclaredType != SymbolType.Date:
                    throw new Exception($"Argument {i + 1} for function {function.Identifier} must be a date.");
            }
        }
    }
    
    private IValue HandleTransform(MashdValue mashd, List<ExpressionNode> arguments)
    {
        if (mashd.Transform is not null)
            throw new Exception("transform() cannot be called twice on the same mashd object");
        
        if (arguments.Count != 1)
            throw new Exception("transform() requires exactly one argument");
        
        if (arguments[0] is not ObjectExpressionNode objectExpression)
            throw new Exception("transform() argument must be an object expression defining the transformation");
        
        mashd.SetTransform(objectExpression);
        
        return mashd;
    }
    
    private IValue HandleJoin(MashdValue mashd)
    {
        var leftData = mashd.LeftDataset.Data;
        var rightData = mashd.RightDataset.Data;

        var outputRows = new List<Dictionary<string, object>>();

        if (mashd.Conditions.Count == 0)
        {
            foreach (var leftRow in leftData)
            {
                foreach (var rightRow in rightData)
                {
                    var joinedRow = new Dictionary<string, object>();

                    if (mashd.Transform is not null)
                    {
                        joinedRow = ApplyTransformation(mashd, leftRow, rightRow, mashd.Transform);
                    }
                    else
                    {
                        foreach (var kvp in leftRow)
                            joinedRow[$"{mashd.LeftIdentifier}.{kvp.Key}"] = kvp.Value;
                        foreach (var kvp in rightRow)
                            joinedRow[$"{mashd.RightIdentifier}.{kvp.Key}"] = kvp.Value;
                    }

                    outputRows.Add(joinedRow);
                }
            }
        }
        else
        {
            foreach (var leftRow in leftData)
            {
                var matchingRightRows = FindMatchingRows(leftRow, rightData, mashd.Conditions);

                foreach (var rightRow in matchingRightRows)
                {
                    var joinedRow = new Dictionary<string, object>();

                    if (mashd.Transform is not null)
                    {
                        joinedRow = ApplyTransformation(mashd, leftRow, rightRow, mashd.Transform);
                    }
                    else
                    {
                        foreach (var kvp in leftRow)
                            joinedRow[$"{mashd.LeftIdentifier}.{kvp.Key}"] = kvp.Value;
                        foreach (var kvp in rightRow)
                            joinedRow[$"{mashd.RightIdentifier}.{kvp.Key}"] = kvp.Value;
                    }

                    outputRows.Add(joinedRow);
                }
            }
        }

        var outputSchema = GenerateSchema(mashd, outputRows);

        return new DatasetValue(outputSchema, outputRows);
    }
    
    private IValue HandleUnion(MashdValue mashd)
    {
        var leftData = mashd.LeftDataset.Data;
        var rightData = mashd.RightDataset.Data;
        
        var outputRows = new List<Dictionary<string, object>>();
        
        if (mashd.Conditions.Count > 0)
        {
            var processedLeftRows = new HashSet<int>();
            var processedRightRows = new HashSet<int>();
        
            for (int leftIndex = 0; leftIndex < leftData.Count; leftIndex++)
            {
                var leftRow = leftData[leftIndex];
                
                for (int rightIndex = 0; rightIndex < rightData.Count; rightIndex++)
                {
                    var rightRow = rightData[rightIndex];
                    
                    bool allMatch = true;
                    foreach (var condition in mashd.Conditions)
                    {
                        bool conditionMatches = condition switch
                        {
                            MatchCondition match => CheckExactMatch(leftRow, rightRow, match),
                            FuzzyMatchCondition fuzzy => CheckFuzzyMatch(leftRow, rightRow, fuzzy),
                            FunctionMatchCondition function => CheckFunctionMatch(leftRow, rightRow, function),
                            _ => false
                        };
                        
                        if (!conditionMatches)
                        {
                            allMatch = false;
                            break;
                        }
                    }
                    
                    if (allMatch)
                    {
                        if (!processedLeftRows.Contains(leftIndex))
                        {
                            if (mashd.Transform is not null)
                            {
                                var transformedRow = ApplyTransformationForUnion(mashd, leftRow, true);
                                outputRows.Add(transformedRow);
                            }
                            else
                            {
                                outputRows.Add(leftRow);
                            }
                            processedLeftRows.Add(leftIndex);
                        }
                        
                        if (!processedRightRows.Contains(rightIndex))
                        {
                            if (mashd.Transform is not null)
                            {
                                var transformedRow = ApplyTransformationForUnion(mashd, rightRow, false);
                                outputRows.Add(transformedRow);
                            }
                            else
                            {
                                outputRows.Add(rightRow);
                            }
                            processedRightRows.Add(rightIndex);
                        }
                    }
                }
            }
        }
        else
        {
            if (mashd.Transform is not null)
            {
                foreach (var leftRow in leftData)
                {
                    var transformedRow = ApplyTransformationForUnion(mashd, leftRow, true);
                    outputRows.Add(transformedRow);
                }
                
                foreach (var rightRow in rightData)
                {
                    var transformedRow = ApplyTransformationForUnion(mashd, rightRow, false);
                    outputRows.Add(transformedRow);
                }
            }
            else
            {
                if (!AreSchemasCompatibleForUnion(mashd.LeftDataset.Schema, mashd.RightDataset.Schema))
                {
                    throw new Exception("Cannot union datasets with incompatible schemas. Use transform() to align schemas.");
                }
                
                outputRows.AddRange(leftData);
                outputRows.AddRange(rightData);
            }
        }
        
        var outputSchema = GenerateSchema(mashd, outputRows);
        
        return new DatasetValue(outputSchema, outputRows);
    }
    
    private List<Dictionary<string, object>> FindMatchingRows(
        Dictionary<string, object> leftRow,
        List<Dictionary<string, object>> rightData,
        List<ICondition> conditions)
    {
        var matchingRows = new List<Dictionary<string, object>>();
        
        foreach (var rightRow in rightData)
        {
            bool allConditionsMatch = true;
            
            foreach (var condition in conditions)
            {
                bool conditionMatches = condition switch
                {
                    MatchCondition match => CheckExactMatch(leftRow, rightRow, match),
                    FuzzyMatchCondition fuzzy => CheckFuzzyMatch(leftRow, rightRow, fuzzy),
                    FunctionMatchCondition function => CheckFunctionMatch(leftRow, rightRow, function),
                    _ => false
                };
                
                if (!conditionMatches)
                {
                    allConditionsMatch = false;
                    break;
                }
            }
            
            if (allConditionsMatch)
                matchingRows.Add(rightRow);
        }
        
        return matchingRows;
    }

    private bool CheckExactMatch(
        Dictionary<string, object> leftRow,
        Dictionary<string, object> rightRow,
        MatchCondition condition)
    {
        var leftColumnName = GetColumnName(condition.Left);
        var rightColumnName = GetColumnName(condition.Right);
        
        var leftValue = GetColumnValue(leftRow, leftColumnName);
        var rightValue = GetColumnValue(rightRow, rightColumnName);
        
        return ValuesEqual(leftValue, rightValue);
    }

    private bool CheckFuzzyMatch(
        Dictionary<string, object> leftRow,
        Dictionary<string, object> rightRow,
        FuzzyMatchCondition condition)
    {
        var leftColumnName = GetColumnName(condition.Left);
        var rightColumnName = GetColumnName(condition.Right);
        var threshold = condition.Threshold.Raw;
        
        var leftValue = GetColumnValue(leftRow, leftColumnName);
        var rightValue = GetColumnValue(rightRow, rightColumnName);
        
        if (leftValue is TextValue leftText && rightValue is TextValue rightText)
        {
            return FuzzyMatchMethod.FuzzyMatch(leftText.Raw, rightText.Raw, threshold);
        }
        
        return ValuesEqual(leftValue, rightValue);
    }

    private bool CheckFunctionMatch(
        Dictionary<string, object> leftRow,
        Dictionary<string, object> rightRow,
        FunctionMatchCondition condition)
    {
        var functionArguments = new List<IValue>();
        
        foreach (var argument in condition.Function.Arguments)
        {
            var schemaField = GetColumnSchemaFieldValue(argument);
            var columnName = schemaField.Name;
            var row = (argument is PropertyAccessValue prop && prop.Identifier == condition.RightIdentifier)
                ? rightRow
                : leftRow;
            
            var columnValue = GetColumnValue(row, columnName);

            IValue argumentValue = schemaField.Type switch
            {
                SymbolType.Integer => new IntegerValue(Convert.ToInt64(columnValue)),
                SymbolType.Decimal => new DecimalValue(Convert.ToDouble(columnValue)),
                SymbolType.Text => new TextValue(columnValue?.ToString() ?? string.Empty),
                SymbolType.Boolean => new BooleanValue(Convert.ToBoolean(columnValue)),
                SymbolType.Date => new DateValue(Convert.ToDateTime(columnValue)),
                _ => throw new NotImplementedException($"Unsupported type {schemaField.Type} for function argument.")
            };
            
            functionArguments.Add(argumentValue);
        }
        
        var result = ExecuteFunctionWithArguments(condition.Function.Node, functionArguments);
        
        return result is BooleanValue { Raw: true };
    }

    private IValue ExecuteFunctionWithArguments(FunctionDefinitionNode funcDef, List<IValue> args)
    {
        var locals = new Dictionary<IDeclaration, IValue>();
        
        for (int i = 0; i < funcDef.ParameterList.Parameters.Count && i < args.Count; i++)
        {
            locals[funcDef.ParameterList.Parameters[i]] = args[i];
        }
        
        _callStack.Push(locals);
        
        try
        {
            foreach (var statement in funcDef.Body.Statements)
                statement.Accept(this);
            
            return new BooleanValue(false);
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

    private Dictionary<string, object> ApplyTransformation(
        MashdValue mashd,
        Dictionary<string, object> leftRow,
        Dictionary<string, object> rightRow,
        ObjectExpressionNode transformation)
    {
        var transformedRow = new Dictionary<string, object>();
        
        if (transformation.Properties.Count == 0)
            throw new Exception("Transformation object must have at least one property defined.");

        var context = new RowContext(mashd.LeftIdentifier, leftRow, mashd.RightIdentifier, rightRow);
        _rowContextStack.Push(context);
        
        try
        {
            foreach (var (outputColumn, expression) in transformation.Properties)
            {
                var value = expression.Accept(this);
            
                var rawValue = ConvertToRawValue(value);
                
                if (rawValue is not null)
                    transformedRow[outputColumn] = rawValue;
            }
        }
        finally
        {
            _rowContextStack.Pop();
        }
        
        return transformedRow;
    }
    
    private Dictionary<string, object> ApplyTransformationForUnion(
        MashdValue mashd,
        Dictionary<string, object> row,
        bool isLeftDataset)
    {
        var transformedRow = new Dictionary<string, object>();
        
        // Create a context with the appropriate dataset
        var context = isLeftDataset
            ? new RowContext(mashd.LeftIdentifier, row, mashd.RightIdentifier, new Dictionary<string, object>())
            : new RowContext(mashd.LeftIdentifier, new Dictionary<string, object>(), mashd.RightIdentifier, row);
        
        _rowContextStack.Push(context);
        
        try
        {
            foreach (var (outputColumn, expression) in mashd.Transform!.Properties)
            {
                try
                {
                    var value = expression.Accept(this);
                    var rawValue = ConvertToRawValue(value);
                    
                    if (rawValue is not null)
                        transformedRow[outputColumn] = rawValue;
                }
                catch
                {
                    // If expression fails (e.g., accessing non-existent dataset), set null
                    transformedRow[outputColumn] = null;
                }
            }
        }
        finally
        {
            _rowContextStack.Pop();
        }
        
        return transformedRow;
    }

    // Check if schemas are compatible for union
    private bool AreSchemasCompatibleForUnion(SchemaValue leftSchema, SchemaValue rightSchema)
    {
        // For union without transformation, schemas should have the same columns with same types
        var leftFields = leftSchema.Raw;
        var rightFields = rightSchema.Raw;
        
        if (leftFields.Count != rightFields.Count)
            return false;
        
        foreach (var (key, leftField) in leftFields)
        {
            if (!rightFields.TryGetValue(key, out var rightField))
                return false;
            
            // Check if types match
            if (leftField.Type != rightField.Type)
                return false;
            
            // Check if names match (the actual column names in the data)
            if (leftField.Name != rightField.Name)
                return false;
        }
        
        return true;
    }
    
    private object? ConvertToRawValue(IValue value)
    {
        return value switch
        {
            IntegerValue iv => iv.Raw,
            DecimalValue dv => dv.Raw,
            TextValue tv => tv.Raw,
            BooleanValue bv => bv.Raw,
            DateValue dateVal => dateVal.Raw,
            NullValue _ => null,
            _ => throw new Exception($"Cannot convert {value.GetType().Name} to raw value")
        };
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
    
    private string GetColumnName(IValue columnRef)
    {
        if (columnRef is not PropertyAccessValue prop)
            throw new NotImplementedException($"Column reference {columnRef.GetType()} not implemented.");

        return prop.Property;
    }
    
    private SchemaFieldValue GetColumnSchemaFieldValue(IValue columnRef)
    {
        if (columnRef is not PropertyAccessValue prop)
            throw new NotImplementedException($"Column reference {columnRef.GetType()} not implemented.");
        
        return prop.FieldValue;
    }
    
    private object GetColumnValue(Dictionary<string, object> row, string columnName)
    {
        return row.TryGetValue(columnName, out var value) ? value : new NullValue();
    }
    
    private IValue ConvertRawToIValue(object rawValue)
    {
        return rawValue switch
        {
            null => new NullValue(),
            long l => new IntegerValue(l),
            int i => new IntegerValue(i),
            double d => new DecimalValue(d),
            float f => new DecimalValue(f),
            string s => new TextValue(s),
            bool b => new BooleanValue(b),
            DateTime dt => new DateValue(dt),
            _ => throw new Exception($"Cannot convert {rawValue.GetType().Name} to IValue")
        };
    }
    
    private bool ValuesEqual(object left, object right)
    {
        return (left, right) switch
        {
            (IntegerValue l, IntegerValue r) => l.Raw == r.Raw,
            (DecimalValue l, DecimalValue r) => Math.Abs(l.Raw - r.Raw) < Tolerance,
            (TextValue l, TextValue r) => l.Raw == r.Raw,
            (BooleanValue l, BooleanValue r) => l.Raw == r.Raw,
            (NullValue, NullValue) => true,
            _ => false
        };
    }
    
    private SchemaValue GenerateSchema(MashdValue mashd, List<Dictionary<string, object>> outputRows)
    {
        var schemaFields = new Dictionary<string, SchemaFieldValue>();
    
        if (mashd.Transform is not null)
        {
            foreach (var (outputColumn, expression) in mashd.Transform.Properties)
            {
                var fieldType = InferTypeFromExpression(expression, mashd);
            
                if (fieldType == SymbolType.Unknown && outputRows.Count > 0)
                {
                    fieldType = InferTypeFromData(outputColumn, outputRows);
                }
            
                schemaFields[outputColumn] = new SchemaFieldValue(
                    type: fieldType,
                    name: outputColumn
                );
            }
        }
        else
        {
            if (outputRows.Count > 0)
            {
                var firstRow = outputRows.First();
            
                foreach (var (columnName, value) in firstRow)
                {
                    var fieldType = InferTypeFromValue(value);
                    schemaFields[columnName] = new SchemaFieldValue(
                        type: fieldType,
                        name: columnName
                    );
                }
            }
            else
            {
                schemaFields = new Dictionary<string, SchemaFieldValue>(mashd.LeftDataset.Schema.Raw);
            }
        }
    
        return new SchemaValue(schemaFields);
    }
    
    private SymbolType InferTypeFromExpression(ExpressionNode expression, MashdValue mashd)
    {
        switch (expression)
        {
            case LiteralNode literal:
                return literal.InferredType;
                
            case PropertyAccessExpressionNode propertyAccess:
                if (propertyAccess.Left is IdentifierNode identifier)
                {
                    SchemaValue schema = null;
                    if (identifier.Name == mashd.LeftIdentifier)
                        schema = mashd.LeftDataset.Schema;
                    else if (identifier.Name == mashd.RightIdentifier)
                        schema = mashd.RightDataset.Schema;
                    
                    if (schema != null && schema.Raw.TryGetValue(propertyAccess.Property, out var field))
                        return field.Type;
                }
                return SymbolType.Unknown;
                
            case BinaryNode binary:
                if (binary.Operator is OpType.Add or OpType.Subtract or OpType.Multiply or OpType.Divide)
                {
                    var leftType = InferTypeFromExpression(binary.Left, mashd);
                    if (leftType != SymbolType.Unknown)
                        return leftType;
                }
                if (binary.Operator is OpType.LessThan or OpType.GreaterThan or OpType.Equality)
                    return SymbolType.Boolean;
                
                return SymbolType.Unknown;
                
            case FunctionCallNode functionCall:
                return functionCall.InferredType;
                
            case MethodChainExpressionNode methodChain:
                return methodChain.InferredType;
                
            default:
                return SymbolType.Unknown;
        }
    }

    // Infer type from actual data values
    private SymbolType InferTypeFromData(string columnName, List<Dictionary<string, object>> rows)
    {
        foreach (var row in rows)
        {
            if (row.TryGetValue(columnName, out var value) && value != null)
            {
                return InferTypeFromValue(value);
            }
        }
        
        // If all values are null, default to Text
        return SymbolType.Text;
    }

    // Infer type from a single value
    private SymbolType InferTypeFromValue(object value)
    {
        return value switch
        {
            null => SymbolType.Unknown,
            long or int => SymbolType.Integer,
            double or float or decimal => SymbolType.Decimal,
            string => SymbolType.Text,
            bool => SymbolType.Boolean,
            DateTime => SymbolType.Date,
            _ => SymbolType.Unknown
        };
    }
}