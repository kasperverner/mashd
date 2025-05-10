using Antlr4.Runtime;
using Mashd.Frontend.AST.Expressions;
using Mashd.Frontend.AST.Statements;
using Mashd.Frontend.AST.Definitions;

namespace Mashd.Frontend.AST;

public class AstBuilder : MashdBaseVisitor<AstNode>
{
    private readonly ErrorReporter errorReporter;

    public AstBuilder(ErrorReporter errorReporter)
    {
        this.errorReporter = errorReporter;
    }

    // Program Node
    public override AstNode VisitProgram(MashdParser.ProgramContext context)
    {
        var statements = new List<StatementNode>();
        var definitions = new List<DefinitionNode>();
        var imports = new List<ImportNode>();

        // Iterate through each statement in the program rule.
        foreach (var stmtCtx in context.statement())
        {
            StatementNode stmtNode = Visit(stmtCtx) as StatementNode;
            if (stmtNode != null)
            {
                statements.Add(stmtNode);
            }
        }

        // Iterate through each definition in the program rule.
        foreach (var defCtx in context.definition())
        {
            DefinitionNode defNode = Visit(defCtx) as DefinitionNode;
            if (defNode != null)
            {
                definitions.Add(defNode);
            }
        }

        // Iterate through each import in the program rule.
        foreach (var impCtx in context.importStatement())
        {
            ImportNode impNode = Visit(impCtx) as ImportNode;
            if (impNode != null)
            {
                imports.Add(impNode);
            }
        }

        var (line, column, text) = ExtractNodeInfo(context);

        return new ProgramNode(imports, definitions, statements, line, column, text);
    }

    // Import Node

    public override AstNode VisitImportDeclaration(MashdParser.ImportDeclarationContext context)
    {
        var (line, column, text) = ExtractNodeInfo(context);

        // Remove surrounding quotes
        string rawText = context.TEXT().GetText();
        string importPath = rawText.Trim('"');

        return new ImportNode(importPath, line, column, text);
    }

    // Definition Nodes
    public override AstNode VisitFunctionDefinition(MashdParser.FunctionDefinitionContext context)
    {
        var (line, column, text) = ExtractNodeInfo(context);

        SymbolType returnType = ParseVariableType(context.type().GetText());
        string functionName = context.ID().GetText();

        var parameters = Visit(context.formalParameters()) as FormalParameterListNode;

        var body = Visit(context.block()) as BlockNode;

        return new FunctionDefinitionNode(functionName, returnType, parameters, body, line, column, text);
    }

    public override AstNode VisitSchemaDefinition(MashdParser.SchemaDefinitionContext context)
    {
        var identifier = context.ID().GetText();

        var objectNode = Visit(context.expression()) as ObjectExpressionNode;
        
        var (line, column, text) = ExtractNodeInfo(context);

        if (objectNode is null)
        {
            throw new ArgumentException("Expected ObjectExpressionNode, but got null");
        } 
        
        SchemaObjectNode schemaObject = BuildSchemaObject(objectNode);

        return new SchemaDefinitionNode(identifier, schemaObject, line, column, text);
    }

    public override AstNode VisitDatasetDefinition(MashdParser.DatasetDefinitionContext context)
    {
        var identifier = context.ID().GetText();

        var (line, column, text) = ExtractNodeInfo(context);

        var node = Visit(context.expression()) switch
        {
            ObjectExpressionNode objectExpression => new DatasetDefinitionNode(identifier, BuildDatasetObject(objectExpression), line, column, text),
            MethodChainExpressionNode methodChain => new DatasetDefinitionNode(identifier, methodChain, line, column,
                text),
            _ => throw new ArgumentException("Invalid expression type for DatasetDefinition")
        };

        return node;
    }

    public override AstNode VisitMashdDefinition(MashdParser.MashdDefinitionContext context)
    {
        var identifier = context.ID().GetText();

        var combineNode = Visit(context.expression()) as BinaryNode;

        return new MashdDefinitionNode(identifier, combineNode.Left, combineNode.Right, combineNode.Line,
            combineNode.Column, combineNode.Text);
    }


    // Expression Nodes
    public override ExpressionNode VisitFunctionCall(MashdParser.FunctionCallContext context)
    {
        var functionName = context.ID().GetText();
        var arguments = new List<ExpressionNode>();

        if (context.actualParameters() != null)
        {
            foreach (var argCtx in context.actualParameters().expression())
            {
                var arg = Visit(argCtx) as ExpressionNode;
                arguments.Add(arg);
            }
        }

        var (line, column, text) = ExtractNodeInfo(context);

        return new FunctionCallNode(functionName, arguments, line, column, text);
    }

    public override ExpressionNode VisitMethodCallExpression(MashdParser.MethodCallExpressionContext context)
    {
        var target = Visit(context.expression())! as ExpressionNode;
        var methodChain = BuildMethodChain(context.methodChain());

        var (line, column, text) = ExtractNodeInfo(context);
        return new MethodChainExpressionNode(target, methodChain.MethodName, methodChain.Arguments, methodChain.Next,
            line, column, text);
    }

    public override AstNode VisitObjectExpression(MashdParser.ObjectExpressionContext context)
    {
        var (line, column, text) = ExtractNodeInfo(context);
        var objectNode = new ObjectExpressionNode(line, column, text);

        foreach (var pairCtx in context.keyValuePair())
        {
            var key = pairCtx.ID().GetText();
            var value = Visit(pairCtx.expression()) as ExpressionNode;
            if (!objectNode.TryAddProperty(key, value))
            {
                errorReporter.Add(ErrorType.AstBuilder, objectNode, $"Duplicate key in object expression: {key}");
            }
        }

        return objectNode;
    }

    public override AstNode VisitPropertyAccessExpression(MashdParser.PropertyAccessExpressionContext context)
    {
        var left = Visit(context.expression()) as ExpressionNode;
        string property = context.ID().GetText();

        var (line, column, text) = ExtractNodeInfo(context);

        return new PropertyAccessExpressionNode(left, property, line, column, text);
    }

    public override AstNode VisitIdentifierExpression(MashdParser.IdentifierExpressionContext context)
    {
        string name = context.ID().GetText();

        var (line, column, text) = ExtractNodeInfo(context);

        return new IdentifierNode(name, line, column, text);
    }

    public override AstNode VisitParenExpression(MashdParser.ParenExpressionContext context)
    {
        var innerExpression = Visit(context.expression()) as ExpressionNode;

        var (line, column, text) = ExtractNodeInfo(context);

        return new ParenNode(innerExpression, line, column, text);
    }

    public override AstNode VisitFunctionCallExpression(MashdParser.FunctionCallExpressionContext context)
    {
        string functionName = context.functionCall().ID().GetText();

        var arguments = new List<ExpressionNode>();

        if (context.functionCall().actualParameters() != null)
        {
            foreach (var argCtx in context.functionCall().actualParameters().expression())
            {
                var arg = Visit(argCtx) as ExpressionNode;
                arguments.Add(arg);
            }
        }

        var (line, column, text) = ExtractNodeInfo(context);
        return new FunctionCallNode(functionName, arguments, line, column, text);
    }

    public override ExpressionNode VisitDatasetCombineExpression(MashdParser.DatasetCombineExpressionContext context)
    {
        var left = Visit(context.expression(0)) as IdentifierNode;
        var right = Visit(context.expression(1)) as IdentifierNode;

        var (line, column, text) = ExtractNodeInfo(context);

        return new BinaryNode(left, right, OpType.Combine, line, column, text);
    }

    // Unary Operation Nodes

    public override AstNode VisitNegationExpression(MashdParser.NegationExpressionContext context)
    {
        return CreateUnaryOperationNode(context.expression(), OpType.Negation, context);
    }

    public override AstNode VisitNotExpression(MashdParser.NotExpressionContext context)
    {
        return CreateUnaryOperationNode(context.expression(), OpType.Not, context);
    }
    
    // Binary Operation Nodes

    public override AstNode VisitMultiplicativeExpression(MashdParser.MultiplicativeExpressionContext context)
    {
        var op = context.op.Text;
        OpType operatorType = op switch
        {
            "*" => OpType.Multiply,
            "/" => OpType.Divide,
            "%" => OpType.Modulo,
            _ => throw new ArgumentException($"Unknown operator: {op}")
        };
        
        return CreateBinaryOperationNode(context.expression(0), context.expression(1), operatorType, context);
    }

    public override AstNode VisitAdditiveExpression(MashdParser.AdditiveExpressionContext context)
    {
        var op = context.op.Text;
        OpType operatorType = op switch
        {
            "+" => OpType.Add,
            "-" => OpType.Subtract,
            _ => throw new ArgumentException($"Unknown operator: {op}")
        };

        return CreateBinaryOperationNode(context.expression(0), context.expression(1), operatorType, context);
    }
    
    public override AstNode VisitComparisonExpression(MashdParser.ComparisonExpressionContext context)
    {
        var op = context.op.Text;
        OpType operatorType = op switch
        {
            "<" => OpType.LessThan,
            "<=" => OpType.LessThanEqual,
            ">" => OpType.GreaterThan,
            ">=" => OpType.GreaterThanEqual,
            "==" => OpType.Equality,
            "!=" => OpType.Inequality,
            _ => throw new ArgumentException($"Unknown operator: {op}")
        };

        return CreateBinaryOperationNode(context.expression(0), context.expression(1), operatorType, context);
    }
    
    public override AstNode VisitLogicalExpression(MashdParser.LogicalExpressionContext context)
    {
        var op = context.op.Text;
        OpType operatorType = op switch
        {
            "&&" => OpType.LogicalAnd,
            "||" => OpType.LogicalOr,
            _ => throw new ArgumentException($"Unknown operator: {op}")
        };

        return CreateBinaryOperationNode(context.expression(0), context.expression(1), operatorType, context);
    }

    public override AstNode VisitNullishCoalescingExpression(MashdParser.NullishCoalescingExpressionContext context)
    {
        return CreateBinaryOperationNode(context.expression(0), context.expression(1), OpType.NullishCoalescing,
            context);
    }


    // Literal Nodes
    public override AstNode VisitLiteralExpression(MashdParser.LiteralExpressionContext context)
    {
        return Visit(context.literal());
    }

    public override AstNode VisitTypeLitteralExpression(MashdParser.TypeLitteralExpressionContext context)
    {
        return VisitTypeLiteral(context.type());
    }

    public override AstNode VisitIntegerLiteral(MashdParser.IntegerLiteralContext context)
    {
        var (line, column, text) = ExtractNodeInfo(context);

        string intText = context.INTEGER().GetText();
        
        long value = long.Parse(intText);
        return new LiteralNode(value, line, column, text, SymbolType.Integer);
    }


    public override AstNode VisitDecimalLiteral(MashdParser.DecimalLiteralContext context)
    {
        var (line, column, text) = ExtractNodeInfo(context);

        string decText = context.DECIMAL().GetText();

        double value = double.Parse(decText);
        return new LiteralNode(value, line, column, text, SymbolType.Decimal);
    }


    public override AstNode VisitTextLiteral(MashdParser.TextLiteralContext context)
    {
        // Remove surrounding quotes
        string rawText = context.TEXT().GetText();
        string value = rawText.Trim('"');
        var (line, column, text) = ExtractNodeInfo(context);

        return new LiteralNode(value, line, column, text, SymbolType.Text);
    }

    public override AstNode VisitBooleanLiteral(MashdParser.BooleanLiteralContext context)
    {
        var (line, column, text) = ExtractNodeInfo(context);

        string boolText = context.BOOLEAN().GetText();

        bool value = boolText.Equals("true", StringComparison.OrdinalIgnoreCase);
        return new LiteralNode(value, line, column, text, SymbolType.Boolean);
    }

    public override AstNode VisitDateLiteral(MashdParser.DateLiteralContext context)
    {
        var (line, column, text) = ExtractNodeInfo(context);

        string dateText = context.DATE().GetText();

        DateTime value = DateTime.Parse(dateText);
        return new LiteralNode(value, line, column, text, SymbolType.Date);
    }

    public override AstNode VisitNullLiteral(MashdParser.NullLiteralContext context)
    {
        var (line, column, text) = ExtractNodeInfo(context);

        return new LiteralNode(null, line, column, text, SymbolType.Void);
    }

    // Statement Nodes

    public override AstNode VisitVariableDeclaration(MashdParser.VariableDeclarationContext context)
    {
        string typeText = context.type().GetText();
        SymbolType type = ParseVariableType(typeText);

        var identifier = context.ID().GetText();

        // Check if there is an initialization expression
        bool hasInitialization = context.expression() != null;

        // If there is an initialization expression visit it
        var expression = hasInitialization ? Visit(context.expression()) as ExpressionNode : null;

        return new VariableDeclarationNode(type, identifier, expression, hasInitialization, context.Start.Line,
            context.Start.Column, context.GetText());
    }

    public override AstNode VisitIfDefinition(MashdParser.IfDefinitionContext context)
    {
        var (line, column, text) = ExtractNodeInfo(context);
        var condition = Visit(context.expression()) as ExpressionNode;
        var ifBlock = Visit(context.block(0)) as BlockNode;
        BlockNode elseBlock = null;
        bool hasElse = false;
        if (context.block().Length > 1)
        {
            elseBlock = Visit(context.block(1)) as BlockNode;
            hasElse = true;
        }

        return new IfNode(condition, ifBlock, elseBlock, hasElse, line, column, text);
    }

    public override AstNode VisitTernaryExpression(MashdParser.TernaryExpressionContext context)
    {
        var (line, column, text) = ExtractNodeInfo(context);
        var condition = Visit(context.expression(0)) as ExpressionNode;
        var ifBlock = Visit(context.expression(1)) as ExpressionNode;
        var elseBlock = Visit(context.expression(2)) as ExpressionNode;

        return new TernaryNode(condition, ifBlock, elseBlock, line, column, text);
    }

    public override AstNode VisitReturnStatement(MashdParser.ReturnStatementContext context)
    {
        var expr = (ExpressionNode)Visit(context.expression());
        var (line, column, text) = ExtractNodeInfo(context);
        return new ReturnNode(expr, line, column, text);
    }


    public override AstNode VisitAssignment(MashdParser.AssignmentContext context)
    {
        var (line, column, text) = ExtractNodeInfo(context);

        string name = context.ID().GetText();
        var expression = Visit(context.expression()) as ExpressionNode;

        return new AssignmentNode(name, expression, line, column, text);
    }

    public override AstNode VisitExpressionStatement(MashdParser.ExpressionStatementContext context)
    {
        var (line, column, text) = ExtractNodeInfo(context);
        
        
        return new ExpressionStatementNode(
            Visit(context.expression()) as ExpressionNode,
            line,
            column,
            text
        );
    }

    // Other Nodes

    public override AstNode VisitBlockDefinition(MashdParser.BlockDefinitionContext context)
    {
        var statements = new List<StatementNode>();
        var (line, column, text) = ExtractNodeInfo(context);

        foreach (var stmtCtx in context.statement())
        {
            var stmt = (StatementNode)Visit(stmtCtx);
            if (stmt != null)
            {
                statements.Add(stmt);
            }
        }

        return new BlockNode(statements, line, column, text);
    }

    public override AstNode VisitParameterList(MashdParser.ParameterListContext context)
    {
        // List to hold individual parameter nodes.
        var paramNodes = new List<FormalParameterNode>();
        var (line, column, text) = ExtractNodeInfo(context);

        if (context != null)
        {
            var types = context.type();
            var ids = context.ID();

            for (int i = 0; i < types.Length; i++)
            {
                SymbolType type = ParseVariableType(types[i].GetText());
                string name = ids[i].GetText();
                var (lineChild, columnChild, textChild) = ExtractNodeInfo(types[i]);

                var paramNode = new FormalParameterNode(type, name, lineChild, columnChild, textChild);
                paramNodes.Add(paramNode);
            }
        }

        return new FormalParameterListNode(paramNodes, line, column, text);
    }

    // Helper Methods
    private (int line, int column, string text) ExtractNodeInfo(ParserRuleContext context)
    {
        return (context.Start.Line, context.Start.Column, context.GetText());
    }

    private ExpressionNode VisitTypeLiteral(MashdParser.TypeContext context)
    {
        var (line, column, text) = ExtractNodeInfo(context);
        string typeText = context.GetText();
        SymbolType type = ParseVariableType(typeText);

        return new LiteralNode(type, line, column, text, type);
    }

    private SymbolType ParseVariableType(string typeText)
    {
        return typeText switch
        {
            "Boolean" => SymbolType.Boolean,
            "Integer" => SymbolType.Integer,
            "Decimal" => SymbolType.Decimal,
            "Text" => SymbolType.Text,
            "Mashd" => SymbolType.Mashd,
            "Date" => SymbolType.Date,
            "Dataset" => SymbolType.Dataset,
            "Schema" => SymbolType.Schema,
            _ => SymbolType.Unknown
        };
    }

    private AstNode CreateUnaryOperationNode(ParserRuleContext expressionContext, OpType operatorType,
        ParserRuleContext context)
    {
        var expression = Visit(expressionContext) as ExpressionNode;

        return new UnaryNode(expression, operatorType, context.Start.Line, context.Start.Column, context.GetText());
    }

    private AstNode CreateBinaryOperationNode(ParserRuleContext leftContext, ParserRuleContext rightContext, OpType op,
        ParserRuleContext context)
    {
        var left = Visit(leftContext) as ExpressionNode;

        var right = Visit(rightContext) as ExpressionNode;

        return new BinaryNode(left, right, op, context.Start.Line, context.Start.Column, context.GetText());
    }

    private MethodChainExpressionNode BuildMethodChain(MashdParser.MethodChainContext context)
    {
        var methodCall = context.functionCall();
        var methodName = methodCall.ID().GetText();

        var arguments = new List<ExpressionNode>();
        if (methodCall.actualParameters() != null)
        {
            foreach (var argCtx in methodCall.actualParameters().expression())
            {
                var arg = Visit(argCtx) as ExpressionNode;
                if (arg != null)
                {
                    arguments.Add(arg);
                }
            }
        }

        var (line, column, text) = ExtractNodeInfo(context);

        MethodChainExpressionNode? next = null;
        if (context.methodChain() != null)
        {
            next = BuildMethodChain(context.methodChain());
        }

        return new MethodChainExpressionNode(null!, methodName, arguments, next, line, column, text);
    }

    private SchemaObjectNode BuildSchemaObject(ObjectExpressionNode node)
    {
        var properties = new Dictionary<string, SchemaField>();

        foreach (var pair in node.Properties)
        {
            var key = pair.Key;

            if (pair.Value is not ObjectExpressionNode value)
            {
                errorReporter.Add(ErrorType.AstBuilder, node, $"Expected value to be ObjectExpressionNode, but got {pair.Value.GetType()}");
                continue;
            }

            var type = value.Properties["type"].Text;
            var name = value.Properties["name"].Text;

            if (string.IsNullOrEmpty(type))
            {
                errorReporter.Add(ErrorType.AstBuilder, node, $"Expected type to be specified in ObjectExpressionNode, but got empty string");
                continue;
            }
            
            if (string.IsNullOrEmpty(name))
            {
                errorReporter.Add(ErrorType.AstBuilder, node, $"Expected name to be specified in ObjectExpressionNode, but got empty string");
                continue;
            }
            
            properties[key] = new SchemaField(type, name);
        }

        return new SchemaObjectNode(properties, node.Line, node.Column, node.Text);
    }
    
    private DatasetObjectNode BuildDatasetObject(ObjectExpressionNode node)
    {
        var properties = new Dictionary<string, DatasetObjectNode.DatasetProperty>();

        foreach (var pair in node.Properties)
        {
            var key = pair.Key;
            var value = pair.Value;

            properties[key] = new DatasetObjectNode.DatasetProperty(key, value);
        }

        return new DatasetObjectNode(node.Line, node.Column, node.Text, properties);
    }
}