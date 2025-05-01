using System.Linq.Expressions;
using Mashd.Backend.Errors;
using Mashd.Frontend.AST;
using Mashd.Frontend.AST.Definitions;
using Mashd.Frontend.AST.Expressions;
using Mashd.Frontend.AST.Statements;
using Mashd.Frontend.SemanticAnalysis;

namespace Mashd.Backend;

public class Interpreter : IAstVisitor<Value>
{
    // Runtime store: map each declaration to its current Value
    private readonly Dictionary<IDeclaration, Value> _values = new Dictionary<IDeclaration, Value>();

    // Expose values for testing or inspection
    public IReadOnlyDictionary<IDeclaration, Value> Values => _values;

    public Value VisitProgramNode(ProgramNode node)
    {
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
        throw new NotImplementedException();
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
        throw new NotImplementedException();
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
                    ToBoolean(leftVal) && ToBoolean(rightVal)
                );
            case OpType.LogicalOr:
                return new BooleanValue(
                    ToBoolean(leftVal) || ToBoolean(rightVal)
                );
            case OpType.NullishCoalescing:
                return leftVal != null ? leftVal : rightVal;

            default:
                throw new NotImplementedException($"Binary operator {node.Operator} not implemented.");
        }
    }
    
    public Value VisitIdentifierNode(IdentifierNode node)
    {
        if (_values.TryGetValue(node.Definition, out var value))
        {
            return value;
        }

        throw new Exception($"Uninitialized variable '{node.Name}'");
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
        throw new NotImplementedException();
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
        return node.Expression.Accept(this);
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
        throw new NotImplementedException();
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
                    if (ri4.Raw == 0) throw new DivisionByZeroException(node.Right.Line, node.Right.Column, node.Text);
                    return new IntegerValue(li4.Raw / ri4.Raw);
                }

                if (leftVal is DecimalValue lf4 && rightVal is DecimalValue rf4)
                {
                    if (rf4.Raw == 0.0)
                        throw new DivisionByZeroException(node.Right.Line, node.Right.Column, node.Text);
                    return new DecimalValue(lf4.Raw / rf4.Raw);
                }

                break;
            case OpType.Modulo:
                if (leftVal is IntegerValue li5 && rightVal is IntegerValue ri5)
                {
                    if (ri5.Raw == 0) throw new DivisionByZeroException(node.Right.Line, node.Right.Column, node.Text);
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

    private bool ToBoolean(Value v)
    {
        if (v is BooleanValue bv) return bv.Raw;
        throw new Exception($"Expected boolean, got {v.GetType()}");
    }

    // Main entry point for evaluation

    public Value Evaluate(AstNode node)
    {
        return node.Accept(this);
    }
}