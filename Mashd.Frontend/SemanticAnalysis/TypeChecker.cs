using Mashd.Frontend.AST;
using Mashd.Frontend.AST.Definitions;
using Mashd.Frontend.AST.Statements;
using Mashd.Frontend.AST.Expressions;

namespace Mashd.Frontend.TypeChecking;

public class TypeChecker : IAstVisitor<SymbolType>
{
    private readonly ErrorReporter errorReporter;
    private readonly Stack<SymbolType> _returnTypeStack = new Stack<SymbolType>();
    private readonly List<int> _returnCounts = new List<int>();

    public TypeChecker(ErrorReporter errorReporter)
    {
        this.errorReporter = errorReporter;
    }
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
        throw new NotImplementedException();
    }

    public SymbolType VisitFunctionDefinitionNode(FunctionDefinitionNode node)
    {
        // Remember the declared return type
        _returnTypeStack.Push(node.DeclaredType);
        // Remember the number of return statements seen
        _returnCounts.Add(0);
        
        // Type-check the body
        node.Body.Accept(this);

        if (_returnCounts[^1] == 0)
        {
            errorReporter.Report.TypeCheck(node, $"Function '{node.Identifier}' has no return statement");
        }
        
        node.InferredType = node.DeclaredType;
        _returnTypeStack.Pop();
        _returnCounts.RemoveAt(_returnCounts.Count - 1);
        return node.DeclaredType;
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
        
        _returnCounts[_returnCounts.Count - 1]++;

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
        throw new NotImplementedException();
    }

    public SymbolType VisitVariableDeclarationNode(VariableDeclarationNode node)
    {
        if (node.HasInitialization)
        {
            var initType = node.Expression.Accept(this);
            if (initType != node.DeclaredType)
            {
                errorReporter.Report.TypeCheck(node, $"Cannot assign {initType} to variable of type {node.DeclaredType}");
            }
        }

        node.InferredType = node.DeclaredType;
        return node.DeclaredType;
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

    public SymbolType VisitCompoundAssignmentNode(CompoundAssignmentNode node)
    {
        var targetType = (node.Definition).DeclaredType;
        if (targetType != SymbolType.Integer && targetType != SymbolType.Decimal)
        {
            errorReporter.Report.TypeCheck(node, $"Compound assignment requires numeric target");
        }

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
            Console.WriteLine(cond);
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
            errorReporter.Report.TypeCheck(node, $"Function '{fnDecl.Identifier}' expects {paramList.Count} args");
        }

        for (int i = 0; i < node.Arguments.Count; i++)
        {
            var argType = node.Arguments[i].Accept(this);
            var expected = paramList[i].DeclaredType;
            if (argType != expected)
            {
                errorReporter.Report.TypeCheck(node, $"Argument {i + 1} has type {argType}, expected {expected}");
            }
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
        node.InferredType = node.ParsedType;
        return node.InferredType;
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
            case OpType.PreIncrement:
            case OpType.PreDecrement:
            case OpType.PostIncrement:
            case OpType.PostDecrement:
                if (exprType != SymbolType.Integer)
                {
                    errorReporter.Report.TypeCheck(node, $"Increment/decrement requires integer operand");
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
                    errorReporter.Report.TypeCheck(node, $"Equality requires basic type operands");
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
                throw new NotImplementedException($"Line {node.Line}:{node.Column}: Nullish coalescing not implemented");
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
        throw new NotImplementedException($"Line {node.Line}:{node.Column}: Property access expression not implemented");
    }

    public SymbolType VisitMethodChainExpressionNode(MethodChainExpressionNode node)
    {
        throw new NotImplementedException($"Line {node.Line}:{node.Column}: Method chain expression not implemented");
    }

    public SymbolType VisitDatasetCombineExpressionNode(DatasetCombineExpressionNode node)
    {
        throw new NotImplementedException($"Line {node.Line}:{node.Column}: Dataset combination not implemented");
    }

    public SymbolType VisitObjectExpressionNode(ObjectExpressionNode node)
    {
        throw new NotImplementedException($"Line {node.Line}:{node.Column}: Object expression not implemented");
    }

    public SymbolType VisitDateLiteralNode(DateLiteralNode node)
    {
        node.InferredType = SymbolType.Date;
        return SymbolType.Date;
    }

    public SymbolType VisitMashdSchemaNode(MashdSchemaNode node)
    {
        throw new NotImplementedException($"Line {node.Line}:{node.Column}: Schema not implemented");
    }

    public SymbolType VisitDatasetLiteralNode(DatasetLiteralNode node)
    {
        throw new NotImplementedException($"Line {node.Line}:{node.Column}: Dataset not implemented");
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
    
    public void Check(AstNode node)
    {
        node.Accept(this);
    }
}