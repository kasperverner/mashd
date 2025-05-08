using Mashd.Frontend.AST;
using Mashd.Frontend.AST.Definitions;
using Mashd.Frontend.AST.Statements;
using Mashd.Frontend.AST.Expressions;

namespace Mashd.Frontend.SemanticAnalysis;

public class TypeChecker : IAstVisitor<SymbolType>
{
    private readonly ErrorReporter errorReporter;
    private readonly Stack<SymbolType> _returnTypeStack = new Stack<SymbolType>();

    public TypeChecker(ErrorReporter errorReporter)
    {
        this.errorReporter = errorReporter;
    }

    public SymbolType VisitProgramNode(ProgramNode node)
    {
        // Type-check all top-level functions
        foreach (var fn in node.Definitions.OfType<FunctionDefinitionNode>())
            fn.Accept(this);

        foreach (var ds in node.Definitions.OfType<DatasetDefinitionNode>())
            ds.Accept(this);

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

    public SymbolType VisitSchemaDefinitionNode(SchemaDefinitionNode node)
    {
        // Check the schemas object node's properties and structure
        node.ObjectNode.Accept(this);

        node.InferredType = SymbolType.Schema;
        return SymbolType.Schema;
    }

    public SymbolType VisitDatasetDefinitionNode(DatasetDefinitionNode node)
    {
        // Check the dataset object node's properties and structure
        if (node.ObjectNode != null)
        {
            node.ObjectNode.Accept(this);
        }

        node.InferredType = SymbolType.Dataset;
        return SymbolType.Dataset;
    }

    public SymbolType VisitMashdDefinitionNode(MashdDefinitionNode node)
    {
        // Check the left and right expressions
        var leftType = node.Left.Accept(this);
        var rightType = node.Right.Accept(this);

        // Both sides should be datasets for a valid mashd operation
        if (leftType != SymbolType.Dataset)
        {
            errorReporter.Report.TypeCheck(
                node,
                $"Left side of mashd definition '{node.Identifier}' must be a Dataset, but got {leftType}"
            );
        }

        if (rightType != SymbolType.Dataset)
        {
            errorReporter.Report.TypeCheck(
                node,
                $"Right side of mashd definition '{node.Identifier}' must be a Dataset, but got {rightType}"
            );
        }

        // The result of a mashd operation is a Mashd type
        node.InferredType = SymbolType.Mashd;
        return SymbolType.Mashd;
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

        // TODO: Check if the method is valid for the left type
        // TODO: Visit the next method in the chain if any

        node.InferredType = leftType;
        return leftType;
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

    public SymbolType VisitSchemaObjectNode(SchemaObjectNode objectNode)
    {
        // Check for invalid field types
        foreach (var (fieldKey, field) in objectNode.Fields)
        {
            SymbolType resolvedType = TryResolveType(field.Type);
            if (!IsBasicType(resolvedType))
            {
                errorReporter.Report.TypeCheck(
                    objectNode,
                    $"Unknown type '{field.Type}' in field '{fieldKey}'"
                );
            }
        }

        objectNode.InferredType = SymbolType.Schema;
        return SymbolType.Schema;
    }

    public SymbolType VisitDatasetObjectNode(DatasetObjectNode node)
    {
        var validNames = new[] { "adapter", "source", "schema", "delimiter", "query", "skip" };
        var requiredNames = new[] { "adapter", "source", "schema" };
        var allowedAdapters = new[] { "csv", "sql", "postgresql",  }; // TODO: Change to actual adapters

        // collect all keys as lowercase for uniform comparison
        var keys = node.Properties.Values
            .Select(p => p.Key.ToLower())
            .ToList();

        // 1) required‑missing
        foreach (var req in requiredNames)
        {
            if (!keys.Contains(req, StringComparer.OrdinalIgnoreCase))
            {
                errorReporter.Report.TypeCheck(node, $"Required property '{req}' is missing");
            }
        }

        // 2) unknown keys
        foreach (var key in keys.Distinct())
        {
            if (!validNames.Contains(key, StringComparer.OrdinalIgnoreCase))
            {
                errorReporter.Report.TypeCheck(node,$"Unknown property '{key}' in dataset");
            }

        }

        // 3) duplicate keys
        var dupes = keys
            .GroupBy(k => k)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key);
        foreach (var dup in dupes)
        {
            errorReporter.Report.TypeCheck(
                node,
                $"Duplicate property '{dup}' in dataset");
        }

        // 4) per‑property semantic/type checks
        foreach (var property in node.Properties.Values)
        {
            var key = property.Key;
            var value = property.Value;

            switch (key.ToLower())
            {
                case "adapter":
                    if (value is string adapterText)
                    {
                        if (!allowedAdapters.Contains(adapterText, StringComparer.OrdinalIgnoreCase))
                        {
                            errorReporter.Report.TypeCheck(node,
                                $"Unsupported adapter '{adapterText}'. Allowed: {string.Join(", ", allowedAdapters)}");
                        }
                    }
                    else
                    {
                        errorReporter.Report.TypeCheck(node, $"Property 'adapter' must be a string literal");
                    }

                    break;

                case "schema":
                    if (node.ResolvedSchema == null)
                    {
                        errorReporter.Report.TypeCheck(node, $"Referenced schema was not resolved");
                    }

                    break;

                case "skip":
                    var skipType = InferPropertyType(value);
                    if (skipType != SymbolType.Integer)
                    {
                        errorReporter.Report.TypeCheck(node, $"Property 'skip' must be an Integer");
                    }

                    break;

                case "source":
                case "delimiter":
                case "query":
                    var t = InferPropertyType(value);
                    if (t != SymbolType.Text)
                    {
                        errorReporter.Report.TypeCheck(node, $"Property '{key}' must be Text");
                    }

                    break;
            }
        }

        node.InferredType = SymbolType.Dataset;
        return SymbolType.Dataset;
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
        foreach (var stmt in block.Statements)
        {
            if (StatementAlwaysReturns(stmt))
            {
                return true; // once a return is hit, subsequent stmts unreachable
            }
        }

        return false;
    }

    public void Check(AstNode node)
    {
        node.Accept(this);
    }

    private SymbolType TryResolveType(string typeName)
    {
        return typeName.ToLower() switch
        {
            "integer" => SymbolType.Integer,
            "decimal" => SymbolType.Decimal,
            "text" => SymbolType.Text,
            "boolean" => SymbolType.Boolean,
            "date" => SymbolType.Date,
            _ => SymbolType.Unknown
        };
    }

    private SymbolType InferPropertyType(object value)
    {
        return value switch
        {
            int => SymbolType.Integer,
            decimal => SymbolType.Decimal,
            double => SymbolType.Decimal,
            float => SymbolType.Decimal,
            string => SymbolType.Text,
            bool => SymbolType.Boolean,
            DateTime => SymbolType.Date,
            _ => SymbolType.Unknown
        };
    }
}