using System.Globalization;
using Mashd.Backend.BuiltInMethods;
using Mashd.Backend.Errors;
using Mashd.Backend.Match;
using Mashd.Backend.Value;
using Mashd.Frontend.AST;
using Mashd.Frontend.AST.Definitions;
using Mashd.Frontend.AST.Expressions;
using Mashd.Frontend.AST.Statements;
using Mashd.Frontend.SemanticAnalysis;

namespace Mashd.Backend.Interpretation;

public class MethodChainHandler(
    DatasetHandler datasetHandler,
    CallStackHandler callStackHandler,
    RowContextHandler rowContextHandler,
    IAstVisitor<IValue> visitor)
{
    public IValue HandlePropertyAccessInRowContext(PropertyAccessExpressionNode node)
    {
        if (node.Left is not IdentifierNode identifier)
            throw new Exception("Property access requires an identifier in row context");
        
        var context = rowContextHandler.Peek();
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

        if (variable.Accept(visitor) is not DatasetValue datasetValue)
            throw new Exception("Property access only valid on dataset variables.");
        
        var schema = datasetValue.Schema.Raw[node.Property];
        
        if (!row.TryGetValue(schema.Name, out var rawValue))
        {
            return new NullValue();
        }
        
        return ConvertRawToIValue(rawValue);
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
    
    public IValue InvokeStaticParse(TypeValue tv, IValue argument, MethodChainExpressionNode node)
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
                    2 when node.Arguments[1].Accept(visitor) is TextValue formatString => Date.parse(dateString.Raw, formatString.Raw),
                    2 => throw new Exception("Date.parse() second argument must be a string format descriptor"),
                    _ => throw new Exception("Date.parse() requires 1 or 2 string arguments")
                };

                return new DateValue(parsed.Value);

            default:
                throw new Exception($"Type '{tv.Raw}' has no static parse()");
        }
    
    }

    public IValue InvokeInstanceMethod(IValue target, string methodName,
        List<ExpressionNode> arguments)
    {
        return target switch
        {
            DatasetValue ds when methodName == "toFile" => datasetHandler.ExportDatasetToFile(ds, arguments),
            DatasetValue ds when methodName == "toTable" => datasetHandler.ExportDatasetToTable(ds, arguments),
            
            MashdValue mv when methodName == "match" => HandleMatch(mv, arguments),
            MashdValue mv when methodName == "fuzzyMatch" => HandleFuzzyMatch(mv, arguments),
            MashdValue mv when methodName == "functionMatch" => HandleFunctionMatch(mv, arguments),
            MashdValue mv when methodName == "transform" => HandleTransform(mv, arguments),
            MashdValue mv when methodName == "join" => HandleJoin(mv),
            MashdValue mv when methodName == "union" => HandleUnion(mv),
            
            _ => throw new Exception("Invalid method call")
        };
    }
    
    private IValue HandleMatch(MashdValue mashd, List<ExpressionNode> arguments)
    {
        if (arguments.Count != 2)
            throw new Exception("match() requires exactly two arguments");

        var left = arguments[0].Accept(visitor);
        var right = arguments[1].Accept(visitor);
        
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

        var left = arguments[0].Accept(visitor);
        var right = arguments[1].Accept(visitor);
        
        if (left is not PropertyAccessValue leftProp)
            throw new Exception("fuzzyMatch() first argument must be a dataset property access");
        
        if (right is not PropertyAccessValue rightProp)
            throw new Exception("fuzzyMatch() second argument must be a dataset property access");

        var threshold = arguments[2].Accept(visitor);
        
        if (threshold is not DecimalValue thresholdValue)
            throw new Exception("fuzzyMatch() third argument must be a decimal value");
        
        mashd.AddCondition(leftProp, rightProp, thresholdValue);

        return mashd;
    }
    
    private IValue HandleFunctionMatch(MashdValue mashd, List<ExpressionNode> arguments)
    {
        var functionDefinition = arguments[0].Accept(visitor) as FunctionDefinitionValue 
            ?? throw new Exception("functionMatch() first argument must be a function definition");
        
        var actualParameters = arguments
            .Skip(1)
            .Select(x => x.Accept(visitor))
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
    
        foreach (var leftRow in leftData)
        {
            var rightRows = mashd.Conditions.Count == 0 
                ? rightData // Cartesian product
                : FindMatchingRows(leftRow, rightData, mashd.Conditions);
    
            foreach (var rightRow in rightRows)
            {
                var joinedRow = mashd.Transform is not null
                    ? ApplyTransformation(mashd, leftRow, rightRow, mashd.Transform)
                    : MergeRows(leftRow, rightRow, mashd.LeftIdentifier, mashd.RightIdentifier);
    
                outputRows.Add(joinedRow);
            }
        }
    
        if (mashd.Conditions.Count == 0 && leftData.Count > 20 && rightData.Count > 20)
        {
            Console.WriteLine($"Warning: Creating Cartesian product of {leftData.Count} x {rightData.Count} = {outputRows.Count} rows");
        }
    
        var outputSchema = GenerateSchema(mashd, outputRows);
        return new DatasetValue(outputSchema, outputRows);
    }
    
    private Dictionary<string, object> MergeRows(
        Dictionary<string, object> leftRow, 
        Dictionary<string, object> rightRow, 
        string leftIdentifier, 
        string rightIdentifier)
    {
        var mergedRow = new Dictionary<string, object>();
    
        foreach (var (key, value) in leftRow)
            mergedRow[$"{leftIdentifier}.{key}"] = value;
    
        foreach (var (key, value) in rightRow)
            mergedRow[$"{rightIdentifier}.{key}"] = value;
    
        return mergedRow;
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
        
                foreach (var (leftIndex, leftRow) in leftData.Select((row, index) => (index, row)))
                {
                    foreach (var (rightIndex, rightRow) in rightData.Select((row, index) => (index, row)))
                    {
                        if (mashd.Conditions.All(condition => condition switch
                        {
                            MatchCondition match => CheckExactMatch(leftRow, rightRow, match),
                            FuzzyMatchCondition fuzzy => CheckFuzzyMatch(leftRow, rightRow, fuzzy),
                            FunctionMatchCondition function => CheckFunctionMatch(leftRow, rightRow, function),
                            _ => false
                        }))
                        {
                            if (processedLeftRows.Add(leftIndex))
                                outputRows.Add(mashd.Transform is not null
                                    ? ApplyTransformationForUnion(mashd, leftRow, true)
                                    : leftRow);
        
                            if (processedRightRows.Add(rightIndex))
                                outputRows.Add(mashd.Transform is not null
                                    ? ApplyTransformationForUnion(mashd, rightRow, false)
                                    : rightRow);
                        }
                    }
                }
            }
            else
            {
                if (mashd.Transform is not null)
                {
                    outputRows.AddRange(leftData.Select(row => ApplyTransformationForUnion(mashd, row, true)));
                    outputRows.AddRange(rightData.Select(row => ApplyTransformationForUnion(mashd, row, false)));
                }
                else
                {
                    if (!AreSchemasCompatibleForUnion(mashd.LeftDataset.Schema, mashd.RightDataset.Schema))
                        throw new Exception("Cannot union datasets with incompatible schemas. Use transform() to align schemas.");
        
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
            return rightData.Where(rightRow =>
                conditions.All(condition => condition switch
                {
                    MatchCondition match => CheckExactMatch(leftRow, rightRow, match),
                    FuzzyMatchCondition fuzzy => CheckFuzzyMatch(leftRow, rightRow, fuzzy),
                    FunctionMatchCondition function => CheckFunctionMatch(leftRow, rightRow, function),
                    _ => false
                })
            ).ToList();
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
                SymbolType.Text => new TextValue(columnValue.ToString() ?? string.Empty),
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
        
        callStackHandler.Push(locals);
        
        try
        {
            foreach (var statement in funcDef.Body.Statements)
                statement.Accept(visitor);
            
            return new BooleanValue(false);
        }
        catch (FunctionReturnExceptionSignal ret)
        {
            return ret.ReturnValue;
        }
        finally
        {
            callStackHandler.Pop();
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
        rowContextHandler.Push(context);
        
        try
        {
            foreach (var (outputColumn, expression) in transformation.Properties)
            {
                var value = expression.Accept(visitor);
            
                var rawValue = ConvertToRawValue(value);
                
                if (rawValue is not null)
                    transformedRow[outputColumn] = rawValue;
            }
        }
        finally
        {
            rowContextHandler.Pop();
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
        
        rowContextHandler.Push(context);
        
        try
        {
            foreach (var (outputColumn, expression) in mashd.Transform!.Properties)
            {
                try
                {
                    var value = expression.Accept(visitor);
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
            rowContextHandler.Pop();
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
    
    private bool ValuesEqual(object left, object right)
    {
        return (left, right) switch
        {
            (IntegerValue l, IntegerValue r) => l.Raw == r.Raw,
            (DecimalValue l, DecimalValue r) => Math.Abs(l.Raw - r.Raw) < 1e-10, // Allow small floating point tolerance
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
                    SchemaValue? schema = null;
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

    private SymbolType InferTypeFromData(string columnName, List<Dictionary<string, object>> rows)
    {
        foreach (var row in rows)
        {
            if (row.TryGetValue(columnName, out var value))
            {
                return InferTypeFromValue(value);
            }
        }
        
        return SymbolType.Text;
    }
    
    private SymbolType InferTypeFromValue(object value)
    {
        return value switch
        {
            long or int => SymbolType.Integer,
            double or float or decimal => SymbolType.Decimal,
            string => SymbolType.Text,
            bool => SymbolType.Boolean,
            DateTime => SymbolType.Date,
            _ => SymbolType.Unknown
        };
    }
    
    public bool ToBoolean(IValue v, AstNode node)
    {
        if (v is BooleanValue bv) return bv.Raw;
        throw new TypeMismatchException(node);
    }
}