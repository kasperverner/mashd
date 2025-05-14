using Mashd.Frontend.AST;
using Mashd.Frontend.AST.Definitions;
using Mashd.Frontend.AST.Statements;
using Mashd.Frontend.AST.Expressions;
using System.Globalization;

namespace Mashd.Frontend.SemanticAnalysis;

public class TypeChecker(ErrorReporter errorReporter) : IAstVisitor<SymbolType>
{
    private readonly Stack<SymbolType> _returnTypeStack = new();

    public SymbolType VisitProgramNode(ProgramNode node)
    {
        // Type-check all top-level functions
        foreach (var fn in node.Definitions.OfType<FunctionDefinitionNode>())
            fn.Accept(this);
        
        // Type-check top-level statements
        foreach (var stmt in node.Statements)
            stmt.Accept(this);

        // The program itself is 'void'
        return SymbolType.Unknown;
    }

    public SymbolType VisitImportNode(ImportNode node)
    {
        throw new NotImplementedException();
    }

    public SymbolType VisitFormalParameterListNode(FormalParameterListNode node)
    {
        foreach (var param in node.Parameters)
        {
            param.Accept(this);
        }

        node.InferredType = SymbolType.Unknown;
        return SymbolType.Unknown;
    }

    public SymbolType VisitFunctionDefinitionNode(FunctionDefinitionNode node)
    {
        // Remember the declared return type
        _returnTypeStack.Push(node.DeclaredType);

        // Type-check the body
        node.Body.Accept(this);

        // Check if every path in the body returns
        if (!BlockAlwaysReturns(node.Body))
        {
            errorReporter.Report.TypeCheck(node,
                $"Function '{{node.Identifier}}' may exit without returning on some paths");
        }

        _returnTypeStack.Pop();

        node.InferredType = node.DeclaredType;
        return node.DeclaredType;
    }

    public SymbolType VisitExpressionStatementNode(ExpressionStatementNode node)
    {
        // TODO: Do we allow anything but type literals in expression statements?
        // Or do we only allow function calls and method chains?

        // Check if the expression is a valid statement
        if (node.Expression is TypeLiteralNode)
        {
            errorReporter.Report.TypeCheck(node, $"Type literal is not a valid statement expression");
        }

        // Check if the expression is a valid statement
        if (node.Expression is not (MethodChainExpressionNode or FunctionCallNode))
        {
            errorReporter.Report.TypeCheck(node, $"Invalid statement expression");
        }

        node.Expression.Accept(this);

        // The statement itself has no type
        node.InferredType = SymbolType.Void;
        return SymbolType.Void;
    }

    public SymbolType VisitReturnNode(ReturnNode node)
    {
        if (_returnTypeStack.Count == 0)
        {
            errorReporter.Report.TypeCheck(node, $"Return statement outside of function");
        }

        var exprType = node.Expression.Accept(this);
        var expected = _returnTypeStack.Peek();
        if (exprType != expected)
        {
            errorReporter.Report.TypeCheck(node, $"Return type {exprType} does not match expected type {expected}");
        }

        node.InferredType = exprType;
        return exprType;
    }


    public SymbolType VisitBlockNode(BlockNode node)
    {
        foreach (var stmt in node.Statements)
        {
            stmt.Accept(this);
        }

        node.InferredType = SymbolType.Unknown;
        return node.InferredType;
    }

    public SymbolType VisitFormalParameterNode(FormalParameterNode node)
    {
        // Check if the parameter type is valid
        var paramType = node.DeclaredType;
        if (paramType == SymbolType.Unknown)
        {
            errorReporter.Report.TypeCheck(node, $"Unknown type '{paramType.ToString()}'");
        }

        node.InferredType = paramType;
        return paramType;
    }

    public SymbolType VisitVariableDeclarationNode(VariableDeclarationNode node)
    {
        if (node.Expression != null)
        {
            var initType = node.Expression.Accept(this);
            
            if (!IsValidAssignment(node.DeclaredType, initType))
            {
                errorReporter.Report.TypeCheck(node,
                    $"Cannot assign {initType} to variable of type {node.DeclaredType}");
            }
        }

        node.InferredType = node.DeclaredType;
        return node.DeclaredType;
    }
    
    private bool IsValidAssignment(SymbolType declaredType, SymbolType derivedType)
    {
        switch (declaredType)
        {
            case SymbolType.Schema when derivedType == SymbolType.Object:
            case SymbolType.Dataset when derivedType == SymbolType.Object:
            case SymbolType.Dataset when derivedType == SymbolType.Unknown:    
            case SymbolType.Mashd when derivedType == SymbolType.Unknown:
                return true;
            default:
            {
                return declaredType == derivedType;
            }
        }
    }

    public SymbolType VisitAssignmentNode(AssignmentNode node)
    {
        var targetType = (node.Definition).DeclaredType;
        var exprType = node.Expression.Accept(this);
        if (exprType != targetType)
        {
            errorReporter.Report.TypeCheck(node, $"Cannot assign {exprType} to {targetType}");
        }

        node.InferredType = targetType;
        return targetType;
    }

    public SymbolType VisitIfNode(IfNode node)
    {
        var cond = node.Condition.Accept(this);
        if (cond != SymbolType.Boolean)
        {
            errorReporter.Report.TypeCheck(node, $"'if' condition must be Boolean");
        }

        node.ThenBlock.Accept(this);
        if (node.HasElse)
        {
            node.ElseBlock.Accept(this);
        }

        node.InferredType = SymbolType.Unknown;
        return SymbolType.Unknown;
    }

    public SymbolType VisitTernaryNode(TernaryNode node)
    {
        var cond = node.Condition.Accept(this);
        if (cond != SymbolType.Boolean)
        {
            errorReporter.Report.TypeCheck(node, $"Ternary condition must be Boolean");
        }

        var trueType = node.TrueExpression.Accept(this);
        var falseType = node.FalseExpression.Accept(this);
        if (trueType != falseType)
        {
            errorReporter.Report.TypeCheck(node, $"Ternary arms must have same type");
        }

        node.InferredType = trueType;
        return trueType;
    }


    public SymbolType VisitFunctionCallNode(FunctionCallNode node)
    {
        var fnDecl = (FunctionDefinitionNode)node.Definition;
        var paramList = fnDecl.ParameterList.Parameters;

        if (node.Arguments.Count != paramList.Count)
        {
            errorReporter.Report.TypeCheck(node,
                $"Function '{fnDecl.Identifier}' expects {paramList.Count} args, but got {node.Arguments.Count} args");
            node.InferredType = fnDecl.DeclaredType;
            return node.InferredType;
        }

        for (int i = 0; i < paramList.Count; i++)
        {
            var argType = node.Arguments[i].Accept(this);
            var expected = paramList[i].DeclaredType;
            if (argType != expected)
                errorReporter.Report.TypeCheck(node,
                    $"Argument {i + 1} has type {argType}, expected {expected}");
        }


        node.InferredType = fnDecl.DeclaredType;
        return node.InferredType;
    }

    public SymbolType VisitIdentifierNode(IdentifierNode node)
    {
        var declared = node.Definition.DeclaredType;
        node.InferredType = declared;
        return declared;
    }

    public SymbolType VisitParenNode(ParenNode node)
    {
        var exprType = node.InnerExpression.Accept(this);
        node.InferredType = exprType;
        return exprType;
    }

    public SymbolType VisitLiteralNode(LiteralNode node)
    {
        if (node.ParsedType == SymbolType.Date && node.Value is string dateString)
        {
            if (!DateTime.TryParseExact(dateString, "yyyy-MM-dd", null, DateTimeStyles.None, out _))
            {
                errorReporter.Report.TypeCheck(node, $"Invalid date format: {dateString}. Expected ISO 8601 (yyyy-MM-dd).");
            }
        }

        node.InferredType = node.ParsedType;
        return node.InferredType;
    }

    public SymbolType VisitTypeLiteralNode(TypeLiteralNode node)
    {
        var type = node.Type;
        if (type == SymbolType.Unknown)
        {
            errorReporter.Report.TypeCheck(node, $"Unknown type '{type.ToString()}'");
        }

        node.InferredType = type;
        return type;
    }

    public SymbolType VisitUnaryNode(UnaryNode node)
    {
        var exprType = node.Operand.Accept(this);
        switch (node.Operator)
        {
            case OpType.Negation:
                if (exprType != SymbolType.Integer && exprType != SymbolType.Decimal)
                {
                    errorReporter.Report.TypeCheck(node, $"Negation '-' requires numeric operand");
                }

                break;
            case OpType.Not:
                if (exprType != SymbolType.Boolean)
                {
                    errorReporter.Report.TypeCheck(node, $"Not '!' requires Boolean operand");
                }

                break;
            default:
                errorReporter.Report.TypeCheck(node, $"Unknown unary operator");
                break;
        }

        node.InferredType = exprType;
        return exprType;
    }

    public SymbolType VisitBinaryNode(BinaryNode node)
    {
        var left = node.Left.Accept(this);
        var right = node.Right.Accept(this);

        if (left != right)
        {
            errorReporter.Report.TypeCheck(node, $"Binary operator requires both sides of same type");
        }

        var assumedType = left;

        switch (node.Operator)
        {
            // Arithmetic
            case OpType.Multiply:
            case OpType.Divide:
            case OpType.Subtract:
                if (!IsNumeric(assumedType))
                {
                    errorReporter.Report.TypeCheck(node, $"Arithmetic requires numeric operands");
                }

                break;
            case OpType.Modulo:
                if (assumedType != SymbolType.Integer)
                {
                    errorReporter.Report.TypeCheck(node, $"Modulus '%' requires integer operands");
                }

                break;
            case OpType.Add:
                if (!IsNumeric(assumedType) && assumedType != SymbolType.Text)
                {
                    errorReporter.Report.TypeCheck(node, $"Plus '+' requires numeric or text operands");
                }

                break;

            // Comparisons
            case OpType.LessThan:
            case OpType.LessThanEqual:
            case OpType.GreaterThan:
            case OpType.GreaterThanEqual:
                if (!IsNumeric(assumedType))
                {
                    errorReporter.Report.TypeCheck(node, $"Comparison requires numeric operands");
                }

                assumedType = SymbolType.Boolean;
                break;

            // Equality
            case OpType.Equality:
            case OpType.Inequality:
                if (!IsBasicType(assumedType))
                {
                    errorReporter.Report.TypeCheck(node, $"Equality operations requires basic type operands");
                }

                assumedType = SymbolType.Boolean;
                break;

            // Logical
            case OpType.LogicalAnd:
            case OpType.LogicalOr:
                if (assumedType != SymbolType.Boolean)
                {
                    errorReporter.Report.TypeCheck(node, $"Logical operations requires Boolean operands");
                }

                assumedType = SymbolType.Boolean;
                break;

            // Nullish coalescing
            case OpType.NullishCoalescing:
                throw new NotImplementedException(
                    $"Line {node.Line}:{node.Column}: Nullish coalescing not implemented");
                break;
            case OpType.Combine:
                if (left != SymbolType.Dataset || right != SymbolType.Dataset)
                {
                    errorReporter.Report.TypeCheck(node, $"Combine requires two datasets");
                }

                assumedType = SymbolType.Mashd;
                break;
            default:
                errorReporter.Report.TypeCheck(node, $"Unsupported binary operator");
                break;
        }

        node.InferredType = assumedType;
        return node.InferredType;
    }

    public SymbolType VisitPropertyAccessExpressionNode(PropertyAccessExpressionNode node)
    {
        var objectType = node.Left.Accept(this);
        if (objectType != SymbolType.Schema && objectType != SymbolType.Dataset)
        {
            errorReporter.Report.TypeCheck(node, $"Property access requires Schema or Dataset");
        }

        // TODO: Check if the property exists in the schema or dataset
        // TODO: Check if the return type is of the correct type

        return node.InferredType;
    }

    public SymbolType VisitMethodChainExpressionNode(MethodChainExpressionNode node)
    {
        // Visit the left side
        var leftType = node.Left?.Accept(this) ?? SymbolType.Unknown;

        bool isValid = leftType switch
        {
            SymbolType.Integer => node.MethodName == "parse",
            SymbolType.Decimal => node.MethodName == "parse",
            SymbolType.Text => node.MethodName == "parse",
            SymbolType.Boolean => node.MethodName == "parse",
            SymbolType.Date => node.MethodName == "parse",
            SymbolType.Dataset => node.MethodName is "toFile" or "toTable",
            SymbolType.Mashd => node.MethodName is "match" or "fuzzyMatch" or "functionMatch" or "transform" or "join"
                or "union",
            _ => false
        };

        if (!isValid)
        {
            errorReporter.Report.TypeCheck(node, $"Method '{node.MethodName}' is not valid for type '{leftType}'");
        }

        if (leftType == SymbolType.Mashd && node.MethodName is "join" or "union")
            leftType = SymbolType.Dataset;
        
        // TODO: Validate the arguments of the method call

        node.InferredType = leftType;
        return leftType;
    }

    public SymbolType VisitObjectExpressionNode(ObjectExpressionNode node)
    {
        // Check for duplicate keys
        var keys = node.Properties.Select(p => p.Key).ToList();
        var duplicates = keys.GroupBy(k => k).Where(g => g.Count() > 1).Select(g => g.Key);
        foreach (var dup in duplicates)
        {
            errorReporter.Report.TypeCheck(node, $"Duplicate key '{dup}' in object");
        }

        // Check the types of the values
        foreach (var pair in node.Properties)
        {
            var valueType = pair.Value.Accept(this);
            if (valueType == SymbolType.Unknown)
            {
                errorReporter.Report.TypeCheck(node, $"Unknown type for key '{pair.Key}'");
            }
        }

        node.InferredType = SymbolType.Object;
        return SymbolType.Object;
    }

    public SymbolType VisitDateLiteralNode(DateLiteralNode node)
    {
        node.InferredType = SymbolType.Date;
        return SymbolType.Date;
    }   

    // Helper methods
    private bool IsNumeric(SymbolType type)
    {
        return type == SymbolType.Integer || type == SymbolType.Decimal;
    }

    private bool IsBasicType(SymbolType type)
    {
        return type == SymbolType.Integer || type == SymbolType.Decimal || type == SymbolType.Text ||
               type == SymbolType.Boolean || type == SymbolType.Date;
    }

    private bool StatementAlwaysReturns(StatementNode stmt)
    {
        switch (stmt)
        {
            case ReturnNode _:
            {
                return true;
            }

            // An if-statement only always returns if:
            //  (a) it has an else branch, and
            //  (b) both the then-block and the else-block always return.
            case IfNode ifs:
            {
                if (!ifs.HasElse)
                {
                    return false;
                }

                bool thenReturns = BlockAlwaysReturns(ifs.ThenBlock);
                bool elseReturns = BlockAlwaysReturns(ifs.ElseBlock!);

                return thenReturns && elseReturns;
            }
            default:
            {
                return false;
            }
        }
    }

    private bool BlockAlwaysReturns(BlockNode block)
    {
        return block.Statements.Any(StatementAlwaysReturns);
    }

    public void Check(AstNode node)
    {
        node.Accept(this);
    }
}