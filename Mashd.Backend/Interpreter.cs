using Mashd.Backend.Errors;
using Mashd.Frontend.AST;
using Mashd.Frontend.AST.Definitions;
using Mashd.Frontend.AST.Expressions;
using Mashd.Frontend.AST.Statements;
using Mashd.Frontend.SemanticAnalysis;
using System.Globalization;
using System.Linq.Expressions;
using Mashd.Backend.Adapters;

namespace Mashd.Backend;

public class Interpreter : IAstVisitor<Value>
{
    // Tolerance for floating-point comparisons
    private const double TOLERANCE = 1e-10;

    // Runtime store: map each declaration to its current Value
    private readonly Dictionary<IDeclaration, Value> _values = new();

    // Expose values for testing or inspection
    public IReadOnlyDictionary<IDeclaration, Value> Values => _values;

    // New: store function definitions
    private readonly Dictionary<FunctionDefinitionNode, FunctionDefinitionNode> _functions = new();

    // New: an activation record stack for parameters/locals
    private readonly Stack<Dictionary<IDeclaration, Value>> _callStack = new();
    
    public Value VisitProgramNode(ProgramNode node)
    {
        // Phase 1: register every function
        foreach (var def in node.Definitions)
            if (def is FunctionDefinitionNode fn)
            {
                VisitFunctionDefinitionNode(fn);
            }

        // Phase 2: run everything (function definitions return null)
        Value last = new NullValue();
        foreach (var stmt in node.Statements)
        {
            last = stmt.Accept(this);
        }

        return last;
    }

    public Value VisitImportNode(ImportNode node)
    {
        return new NullValue();
    }

    public Value VisitFunctionCallNode(FunctionCallNode node)
    {
        var def = (FunctionDefinitionNode)node.Definition!;

        // evaluate arguments
        var argValues = node.Arguments.Select(a => a.Accept(this)).ToArray();

        // bind parameters into a fresh activation‐record
        var locals = new Dictionary<IDeclaration, Value>();
        for (int i = 0; i < def.ParameterList.Parameters.Count; i++)
        {
            IDeclaration paramDecl = def.ParameterList.Parameters[i];
            locals[paramDecl] = argValues[i];
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

    public Value VisitVariableDeclarationNode(VariableDeclarationNode node)
    {
        var value = node switch
        {
            { DeclaredType: SymbolType.Dataset, Expression: ObjectExpressionNode } => HandleDatasetImport(node),
            // TODO: Implement after method chain
            // { DeclaredType: SymbolType.Dataset, Expression: MethodChainExpressionNode } => HandleDatasetFromMethodChain(node),
            { Expression: not null } => node.Expression.Accept(this),
            _ => new NullValue()
        };
        
        _values[node] = value;
        return value;
    }

    private DatasetValue HandleDatasetImport(VariableDeclarationNode node)
    {
        var value = node.Expression?.Accept(this);
        
        if (value is not ObjectValue objectValue)
            throw new ParseException("Invalid dataset object value.", node.Line, node.Column);
        
        var dataset = BuildDatasetFromObject(node, objectValue);
        ValidateDatasetProperties(node, dataset);
        LoadDatasetData(dataset);
        ValidateDatasetData(node, dataset);
        
        return dataset;
    }

    private DatasetValue BuildDatasetFromObject(VariableDeclarationNode node, ObjectValue value)
    {
        var properties = new Dictionary<string, string>();
        foreach (var (key, val) in value.Raw)
            if (val is TextValue textValue)
                properties[key] = textValue.Raw;

        if (!properties.TryGetValue("source", out var source))
            throw new ParseException($"Dataset {node.Identifier} missing 'source' property.", node.Line, node.Column);

        if (!properties.TryGetValue("adapter", out var adapter))
            throw new ParseException($"Dataset {node.Identifier} missing 'adapter' property.", node.Line, node.Column);

        if (!value.Raw.TryGetValue("schema", out var schemaObject))
            throw new ParseException($"Dataset {node.Identifier} missing 'schema' property.", node.Line, node.Column);

        var schema = BuildSchemaFromObject(schemaObject);
        
        var query = properties.GetValueOrDefault("query");
        var delimiter = properties.GetValueOrDefault("delimiter");
        
        return new DatasetValue(schema, source, adapter, query, delimiter);
    }

    private SchemaValue BuildSchemaFromObject(Value value)
    {
        if (value is not ObjectValue objectValue)
            throw new ArgumentException("Invalid object value for schema.");
        
        var fields = new Dictionary<string, SchemaFieldValue>();

        foreach (var (identifier, fieldValue) in objectValue.Raw)
        {
            if (fieldValue is not ObjectValue fieldObjectValue) continue;
            
            var name = fieldObjectValue.Raw.GetValueOrDefault("name");
            var type = fieldObjectValue.Raw.GetValueOrDefault("type");
                
            if (name is not TextValue textValue || type is not TypeValue typeValue)
                throw new ArgumentException("Invalid object value for schema field.");
                
            fields[identifier] = new SchemaFieldValue(typeValue.Raw, textValue.Raw);
        }

        return new SchemaValue(fields);
    }

    private void ValidateDatasetProperties(VariableDeclarationNode node, DatasetValue dataset)
    {
        if (dataset.Adapter is "sqlserver" or "postgresql" && string.IsNullOrWhiteSpace(dataset.Query))
        {
            throw new ParseException($"Dataset {dataset.Source} missing 'query' property.", node.Line, node.Column);
        }
        
        if (dataset.Adapter is "csv" && string.IsNullOrWhiteSpace(dataset.Delimiter))
        {
            throw new ParseException($"Dataset {dataset.Source} missing 'delimiter' property.", node.Line, node.Column);
        }
        
        if (dataset.Schema.Raw.Count == 0)
        {
            throw new ParseException($"Dataset {dataset.Source} missing 'schema' property.", node.Line, node.Column);
        }
    }

    private void LoadDatasetData(DatasetValue dataset)
    {
        try
        {
            var adapter = AdapterFactory.CreateAdapter(dataset.Adapter, new Dictionary<string, string>
            {
                { "source", dataset.Source },
                { "query", dataset.Query ?? "" },
                { "delimiter", dataset.Delimiter ?? "," }
            });
            
            var data = adapter.ReadAsync().Result;
            dataset.AddData(data);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    private void ValidateDatasetData(VariableDeclarationNode node, DatasetValue dataset)
    {
        // Validate the data matches the defined schema
        var firstRow = dataset.Data.FirstOrDefault();
        if (firstRow == null)
            throw new ParseException($"Dataset {node.Identifier} has no data.", node.Line, node.Column);
        
        foreach (var field in dataset.Schema.Raw)
        {
            if (!firstRow.TryGetValue(field.Value.Name, out var value))
                throw new ParseException($"Dataset {node.Identifier} missing field '{field.Key}'.", node.Line, node.Column);

            try
            {
                switch (field.Value.Type)
                {
                    case SymbolType.Integer:
                        IntegerValue.TryParse(value.ToString());
                        break;
                    case SymbolType.Decimal:
                        DecimalValue.TryParse(value.ToString());
                        break;
                    case SymbolType.Text:
                        TextValue.TryParse(value.ToString());
                        break;
                    case SymbolType.Boolean:
                        BooleanValue.TryParse(value.ToString());
                        break;
                    case SymbolType.Date:
                        DateValue.TryParse(value.ToString());
                        break;
                }
            }
            catch (Exception e)
            {
                throw new ParseException($"Dataset {node.Identifier} has field '{field.Key}' with wrong data type.", node.Line, node.Column);
            }
        }
    }

    private DatasetValue HandleDatasetFromMethodChain(VariableDeclarationNode node)
    {
        var value = node.Expression?.Accept(this);
        
        if (value is not DatasetValue datasetValue)
            throw new ParseException("Invalid dataset method chain value.", node.Line, node.Column);
        
        ValidateDatasetProperties(node, datasetValue);
        LoadDatasetData(datasetValue);
        ValidateDatasetData(node, datasetValue);
        
        return datasetValue;
    }

    public Value VisitAssignmentNode(AssignmentNode node)
    {
        var value = node.Expression.Accept(this);
        _values[node.Definition] = value;
        
        return value;
    }

    public Value VisitIfNode(IfNode node)
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

    public Value VisitTernaryNode(TernaryNode node)
    {
        var conditionValue = node.Condition.Accept(this);
        if (conditionValue is BooleanValue booleanValue)
        {
            return booleanValue.Raw ? node.TrueExpression.Accept(this) : node.FalseExpression.Accept(this);
        }

        throw new TypeMismatchException(node.Condition);
    }

    public Value VisitExpressionStatementNode(ExpressionStatementNode node)
    {
        // TODO: Implement expression statement
        return new NullValue();
    }

    public Value VisitParenNode(ParenNode node)
    {
        return node.InnerExpression.Accept(this);
    }

    public Value VisitLiteralNode(LiteralNode node)
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

    public Value VisitTypeLiteralNode(TypeLiteralNode node)
    {
        return new TypeValue(node.Type);
    }

    public Value VisitUnaryNode(UnaryNode node)
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

    public Value VisitBinaryNode(BinaryNode node)
    {
        var leftVal = node.Left.Accept(this);
        var rightVal = node.Right.Accept(this);

        switch (node.Operator)
        {
            // Arithmetic
            case OpType.Add:
            case OpType.Subtract:
            case OpType.Multiply:
            case OpType.Divide:
            case OpType.Modulo:
                return EvaluateArithmetic(node.Operator, leftVal, rightVal, node);

            // Comparison
            case OpType.LessThan:
            case OpType.LessThanEqual:
            case OpType.GreaterThan:
            case OpType.GreaterThanEqual:
            case OpType.Equality:
            case OpType.Inequality:
                return EvaluateComparison(node.Operator, leftVal, rightVal);

            // Logical & Coalesce (if needed)
            case OpType.LogicalAnd:
                return new BooleanValue(
                    ToBoolean(leftVal, node.Left) && ToBoolean(rightVal, node.Right)
                );
            case OpType.LogicalOr:
                return new BooleanValue(
                    ToBoolean(leftVal, node.Left) || ToBoolean(rightVal, node.Right)
                );
            case OpType.NullishCoalescing:
                return leftVal is NullValue ? rightVal : leftVal;

            case OpType.Combine:
                return new MashdValue(leftVal, rightVal);
            
            default:
                throw new NotImplementedException($"Binary operator {node.Operator} not implemented.");
        }
    }

    public Value VisitIdentifierNode(IdentifierNode node)
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

        throw new UndefinedVariableException(node);
    }

    public Value VisitBlockNode(BlockNode node)
    {
        Value last = new NullValue();
        foreach (var stmt in node.Statements)
        {
            last = stmt.Accept(this);
        }

        return last;
    }

    public Value VisitFormalParameterNode(FormalParameterNode node)
    {
        // TODO: Implement formal parameter
        return new NullValue();
    }

    public Value VisitFormalParameterListNode(FormalParameterListNode node)
    {
        // TODO: Implement formal parameter list
        return new NullValue();
    }

    public Value VisitFunctionDefinitionNode(FunctionDefinitionNode node)
    {
        // register the function AST-node so calls can find it
        _functions[node] = node;
        return new NullValue();
    }
    
    public Value VisitReturnNode(ReturnNode node)
    {
        var value = node.Expression.Accept(this);
        throw new FunctionReturnExceptionSignal(value);
    }

    public Value VisitPropertyAccessExpressionNode(PropertyAccessExpressionNode node)
    {
        // TODO: Implement property access
        return new NullValue();
    }

    public Value VisitMethodChainExpressionNode(MethodChainExpressionNode node)
    {
        // TODO: Implement method chain
        return new NullValue();
    }

    public Value VisitDateLiteralNode(DateLiteralNode node)
    {
        if (DateTime.TryParseExact(node.Text, "yyyy-MM-dd", null, DateTimeStyles.None, out var parsedDate))
        {
            return new DateValue(parsedDate);
        }

        throw new FormatException($"Invalid date format: {node.Text}. Expected ISO 8601 (yyyy-MM-dd).");
    }

    public Value VisitObjectExpressionNode(ObjectExpressionNode node)
    {
        var properties = new Dictionary<string, Value>();
        foreach (var (key, expression) in node.Properties)
        {
            var value = expression.Accept(this);
            
            properties[key] = value;
        }

        return new ObjectValue(properties);
    }
    
    // Helper methods

    private Value EvaluateArithmetic(OpType op, Value leftVal, Value rightVal, BinaryNode node)
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

    private Value EvaluateComparison(OpType op, Value leftVal, Value rightVal)
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
                    OpType.Equality => Math.Abs(lf.Raw - rf.Raw) < TOLERANCE,
                    OpType.Inequality => Math.Abs(lf.Raw - rf.Raw) > TOLERANCE,
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

    private bool ToBoolean(Value v, AstNode node)
    {
        if (v is BooleanValue bv) return bv.Raw;
        throw new TypeMismatchException(node);
    }

    // Main entry point for evaluation

    public Value Evaluate(AstNode node)
    {
        return node.Accept(this);
    }
}