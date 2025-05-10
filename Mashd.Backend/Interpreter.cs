using System.Linq.Expressions;
using Mashd.Backend.Errors;
using Mashd.Frontend.AST;
using Mashd.Frontend.AST.Definitions;
using Mashd.Frontend.AST.Expressions;
using Mashd.Frontend.AST.Statements;
using Mashd.Frontend.SemanticAnalysis;
using System.Globalization;

namespace Mashd.Backend;

public class Interpreter : IAstVisitor<Value>
{
    // Runtime store: map each declaration to its current Value
    private readonly Dictionary<IDeclaration, Value> _values = new Dictionary<IDeclaration, Value>();

    // Expose values for testing or inspection
    public IReadOnlyDictionary<IDeclaration, Value> Values => _values;

    // New: store function definitions
    private readonly Dictionary<FunctionDefinitionNode, FunctionDefinitionNode> _functions
        = new Dictionary<FunctionDefinitionNode, FunctionDefinitionNode>();

    // New: an activation record stack for parameters/locals
    private readonly Stack<Dictionary<IDeclaration, Value>> _callStack
        = new Stack<Dictionary<IDeclaration, Value>>();


    public Value VisitProgramNode(ProgramNode node)
    {
        // Phase 1: register every function
        foreach (var def in node.Definitions)
            if (def is FunctionDefinitionNode fn)
            {
                VisitFunctionDefinitionNode(fn);
            }

        // Phase 2: run everything (function definitions return null)
        Value last = null;
        foreach (var stmt in node.Statements)
        {
            last = stmt.Accept(this);
        }

        return last;
    }

    public Value VisitImportNode(ImportNode node)
    {
        throw new NotImplementedException();
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
        Value value;
        if (node.HasInitialization)
        {
            value = node.Expression.Accept(this);
        }
        else
        {
            // default uninitialized integer to 0, TBD implement properly
            value = new IntegerValue(0L);
        }

        _values[node] = value;
        return value;
    }

    public Value VisitAssignmentNode(AssignmentNode node)
    {
        var value = node.Expression.Accept(this);
        _values[node.Definition] = value;
        return value;
    }

    public Value VisitCompoundAssignmentNode(CompoundAssignmentNode node)
    {
        throw new NotImplementedException();
    }

    public Value VisitIfNode(IfNode node)
    {
        var conditionValue = node.Condition.Accept(this);
        if (conditionValue is BooleanValue booleanValue)
        {
            if (booleanValue.Raw == true)
            {
                return node.ThenBlock.Accept(this);
            }

            if (node.HasElse && booleanValue.Raw == false)
            {
                return node.ElseBlock?.Accept(this);
            }
        }

        return null;
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

    public Value VisitParenNode(ParenNode node)
    {
        return node.InnerExpression.Accept(this);
    }

    public Value VisitLiteralNode(LiteralNode node)
    {
        switch (node.InferredType)
        {
            case SymbolType.Integer:
                return new IntegerValue((long)node.Value);
            case SymbolType.Decimal:
                return new DecimalValue((double)node.Value);
            case SymbolType.Text:
                return new TextValue((string)node.Value);
            case SymbolType.Boolean:
                return new BooleanValue((bool)node.Value);
            default:
                throw new NotImplementedException($"Literal type {node.InferredType} not implemented.");
        }
    }

    public Value VisitUnaryNode(UnaryNode node)
    {
        var value = node.Operand.Accept(this);
        switch (node.Operator)
        {
            case OpType.Negation:
                if (value is IntegerValue iv)
                {
                    return new IntegerValue(-iv.Raw);
                }

                if (value is DecimalValue dv)
                {
                    return new DecimalValue(-dv.Raw);
                }

                break;

            case OpType.Not:
                if (value is BooleanValue bv)
                {
                    return new BooleanValue(!bv.Raw);
                }

                break;
        }

        throw new NotImplementedException(
            $"Unary operator {node.Operator} not implemented for type {value.GetType()}.");
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
                return leftVal != null ? leftVal : rightVal;

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
        Value last = null;
        foreach (var stmt in node.Statements)
        {
            last = stmt.Accept(this);
        }

        return last;
    }

    public Value VisitFormalParameterNode(FormalParameterNode node)
    {
        throw new NotImplementedException();
    }

    public Value VisitFormalParameterListNode(FormalParameterListNode node)
    {
        throw new NotImplementedException();
    }

    public Value VisitFunctionDefinitionNode(FunctionDefinitionNode node)
    {
        // register the function AST-node so calls can find it
        _functions[node] = node;
        return null;
    }

    public Value VisitSchemaDefinitionNode(SchemaDefinitionNode node)
    {
        throw new NotImplementedException();
    }

    public Value VisitDatasetDefinitionNode(DatasetDefinitionNode node)
    {
        throw new NotImplementedException();
    }

    public Value VisitMashdDefinitionNode(MashdDefinitionNode node)
    {
        throw new NotImplementedException();
    }

    public Value VisitReturnNode(ReturnNode node)
    {
        var value = node.Expression.Accept(this);
        throw new FunctionReturnExceptionSignal(value);
    }

    public Value VisitPropertyAccessExpressionNode(PropertyAccessExpressionNode node)
    {
        throw new NotImplementedException();
    }

    public Value VisitMethodChainExpressionNode(MethodChainExpressionNode node)
    {
        throw new NotImplementedException();
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
        throw new NotImplementedException();
    }

    public Value VisitSchemaObjectNode(SchemaObjectNode objectNode)
    {
        throw new NotImplementedException();
    }

    public Value VisitDatasetObjectNode(DatasetObjectNode node)
    {
        throw new NotImplementedException();
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
                    OpType.Equality => lf.Raw == rf.Raw,
                    OpType.Inequality => lf.Raw != rf.Raw,
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