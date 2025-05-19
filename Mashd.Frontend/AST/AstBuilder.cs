using System.Globalization;
using Antlr4.Runtime;
using Mashd.Frontend.AST.Expressions;
using Mashd.Frontend.AST.Statements;
using Mashd.Frontend.AST.Definitions;

namespace Mashd.Frontend.AST;

public class AstBuilder : MashdBaseVisitor<AstNode>
{
    private readonly ErrorReporter _errorReporter;
    private readonly int _level;

    public AstBuilder(ErrorReporter errorReporter, int level)
    {
        _errorReporter = errorReporter;
        _level = level;
        
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

        return new ProgramNode(imports, definitions, statements, line, column, text, _level);
    }

    // Import Node

    public override AstNode VisitImportDeclaration(MashdParser.ImportDeclarationContext context)
    {
        var (line, column, text) = ExtractNodeInfo(context);

        // Remove surrounding quotes
        string rawText = context.TEXT().GetText();
        string importPath = rawText.Trim('"');

        return new ImportNode(importPath, line, column, text, _level);
    }

    // Definition Nodes
    public override AstNode VisitFunctionDefinition(MashdParser.FunctionDefinitionContext context)
    {
        var (line, column, text) = ExtractNodeInfo(context);

        SymbolType returnType = ParseVariableType(context.type().GetText());
        string functionName = context.ID().GetText();

        var parameters = Visit(context.formalParameters()) as FormalParameterListNode;

        var body = Visit(context.block()) as BlockNode;

        return new FunctionDefinitionNode(functionName, returnType, parameters, body, line, column, text, _level);
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

        return new FunctionCallNode(functionName, arguments, line, column, text, _level);
    }

    public override ExpressionNode VisitMethodCallExpression(MashdParser.MethodCallExpressionContext context)
    {
        var target = Visit(context.expression())! as ExpressionNode;
        var methodChain = BuildMethodChain(context.methodChain());

        var (line, column, text) = ExtractNodeInfo(context);
        return new MethodChainExpressionNode(target, methodChain.MethodName, methodChain.Arguments, methodChain.Next,
            line, column, text, _level);
    }

    public override AstNode VisitObjectExpression(MashdParser.ObjectExpressionContext context)
    {
        var (line, column, text) = ExtractNodeInfo(context);
        var objectNode = new ObjectExpressionNode(line, column, text, _level);

        foreach (var pairCtx in context.keyValuePair())
        {
            var key = pairCtx.ID().GetText();
            var value = Visit(pairCtx.expression()) as ExpressionNode;
            if (!objectNode.TryAddProperty(key, value))
            {
                _errorReporter.Add(ErrorType.AstBuilder, objectNode, $"Duplicate key in object expression: {key}");
            }
        }

        return objectNode;
    }

    public override AstNode VisitPropertyAccessExpression(MashdParser.PropertyAccessExpressionContext context)
    {
        var left = Visit(context.expression()) as ExpressionNode;
        string property = context.ID().GetText();

        var (line, column, text) = ExtractNodeInfo(context);

        return new PropertyAccessExpressionNode(left, property, line, column, text, _level);
    }

    public override AstNode VisitIdentifierExpression(MashdParser.IdentifierExpressionContext context)
    {
        string name = context.ID().GetText();

        var (line, column, text) = ExtractNodeInfo(context);

        return new IdentifierNode(name, line, column, text, _level);
    }

    public override AstNode VisitParenExpression(MashdParser.ParenExpressionContext context)
    {
        var innerExpression = Visit(context.expression()) as ExpressionNode;

        var (line, column, text) = ExtractNodeInfo(context);

        return new ParenNode(innerExpression, line, column, text, _level);
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
        return new FunctionCallNode(functionName, arguments, line, column, text, _level);
    }

    public override ExpressionNode VisitDatasetCombineExpression(MashdParser.DatasetCombineExpressionContext context)
    {
        var left = Visit(context.expression(0)) as IdentifierNode;
        var right = Visit(context.expression(1)) as IdentifierNode;

        var (line, column, text) = ExtractNodeInfo(context);

        return new BinaryNode(left, right, OpType.Combine, line, column, text, _level);
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
        var node = Visit(context.literal());
        return node;
    }

    public override AstNode VisitTypeLitteralExpression(MashdParser.TypeLitteralExpressionContext context)
    {
        var (line, column, text) = ExtractNodeInfo(context);
        string typeText = context.GetText();
        SymbolType type = ParseVariableType(typeText);
        return new TypeLiteralNode(type, line, column, text, type, _level);
    }

    public override AstNode VisitIntegerLiteral(MashdParser.IntegerLiteralContext context)
    {
        var (line, column, text) = ExtractNodeInfo(context);

        string intText = context.INTEGER().GetText();

        long value = long.Parse(intText);
        return new LiteralNode(value, line, column, text, SymbolType.Integer, _level);
    }


    public override AstNode VisitDecimalLiteral(MashdParser.DecimalLiteralContext context)
    {
        var (line, column, text) = ExtractNodeInfo(context);

        string decText = context.DECIMAL().GetText();

        double value = double.Parse(decText, CultureInfo.InvariantCulture);
        return new LiteralNode(value, line, column, text, SymbolType.Decimal, _level);
    }


    public override AstNode VisitTextLiteral(MashdParser.TextLiteralContext context)
    {
        // Remove surrounding quotes
        string rawText = context.TEXT().GetText();
        string value = rawText.Trim('"');
        var (line, column, text) = ExtractNodeInfo(context);
        return new LiteralNode(value, line, column, text, SymbolType.Text, _level);
    }

    public override AstNode VisitBooleanLiteral(MashdParser.BooleanLiteralContext context)
    {
        var (line, column, text) = ExtractNodeInfo(context);

        string boolText = context.BOOLEAN().GetText();

        bool value = boolText.Equals("true", StringComparison.OrdinalIgnoreCase);
        return new LiteralNode(value, line, column, text, SymbolType.Boolean, _level);
    }

    public override AstNode VisitDateLiteral(MashdParser.DateLiteralContext context)
    {
        var (line, column, text) = ExtractNodeInfo(context);

        string dateText = context.DATE().GetText();

        DateTime value = DateTime.Parse(dateText);
        return new LiteralNode(value, line, column, text, SymbolType.Date, _level);
    }

    public override AstNode VisitNullLiteral(MashdParser.NullLiteralContext context)
    {
        var (line, column, text) = ExtractNodeInfo(context);

        return new LiteralNode(null, line, column, text, SymbolType.Void, _level);
    }

    // Statement Nodes

    public override AstNode VisitVariableDeclaration(MashdParser.VariableDeclarationContext context)
    {
        string typeText = context.type().GetText();
        SymbolType type = ParseVariableType(typeText);

        var identifier = context.ID().GetText();

        var expression = context.expression() is { } exprCtx
            ? Visit(exprCtx) as ExpressionNode
            : null;

        var (line, column, text) = ExtractNodeInfo(context);

        return new VariableDeclarationNode(type, identifier, expression, line, column, text, _level);
    }

    public override AstNode VisitIfDefinition(MashdParser.IfDefinitionContext context)
    {
        var (line, column, text) = ExtractNodeInfo(context);
        var condition = (ExpressionNode)Visit(context.expression());
        var thenBlock = (BlockNode)Visit(context.block(0));

        BlockNode elseBlock = null;
        bool hasElse = false;

        var elifCtx = context.@if();
        if (elifCtx != null)
        {
            var nestedIf = (IfNode)Visit(elifCtx);

            elseBlock = new BlockNode(
                new List<StatementNode> { nestedIf },
                line, column, text, _level
            );
            hasElse = true;
        }

        else if (context.block().Length > 1)
        {
            elseBlock = (BlockNode)Visit(context.block(1));
            hasElse = true;
        }

        return new IfNode(condition, thenBlock, elseBlock, hasElse, line, column, text, _level);
    }


    public override AstNode VisitTernaryExpression(MashdParser.TernaryExpressionContext context)
    {
        var (line, column, text) = ExtractNodeInfo(context);
        var condition = Visit(context.expression(0)) as ExpressionNode;
        var ifBlock = Visit(context.expression(1)) as ExpressionNode;
        var elseBlock = Visit(context.expression(2)) as ExpressionNode;

        return new TernaryNode(condition, ifBlock, elseBlock, line, column, text, _level);
    }

    public override AstNode VisitReturnStatement(MashdParser.ReturnStatementContext context)
    {
        var expr = (ExpressionNode)Visit(context.expression());
        var (line, column, text) = ExtractNodeInfo(context);
        return new ReturnNode(expr, line, column, text, _level);
    }


    public override AstNode VisitAssignment(MashdParser.AssignmentContext context)
    {
        var (line, column, text) = ExtractNodeInfo(context);

        string name = context.ID().GetText();
        var expression = Visit(context.expression()) as ExpressionNode;

        return new AssignmentNode(name, expression, line, column, text, _level);
    }

    public override AstNode VisitExpressionStatement(MashdParser.ExpressionStatementContext context)
    {
        var (line, column, text) = ExtractNodeInfo(context);


        return new ExpressionStatementNode(
            Visit(context.expression()) as ExpressionNode,
            line,
            column,
            text,
            _level
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

        return new BlockNode(statements, line, column, text, _level);
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

                var paramNode = new FormalParameterNode(type, name, lineChild, columnChild, textChild, _level);
                paramNodes.Add(paramNode);
            }
        }

        return new FormalParameterListNode(paramNodes, line, column, text, _level);
    }

    // Helper Methods
    private (int line, int column, string text) ExtractNodeInfo(ParserRuleContext context)
    {
        return (context.Start.Line, context.Start.Column, context.GetText());
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

        return new UnaryNode(expression, operatorType, context.Start.Line, context.Start.Column, context.GetText(), _level);
    }

    private AstNode CreateBinaryOperationNode(ParserRuleContext leftContext, ParserRuleContext rightContext, OpType op,
        ParserRuleContext context)
    {
        var left = Visit(leftContext) as ExpressionNode;

        var right = Visit(rightContext) as ExpressionNode;

        return new BinaryNode(left, right, op, context.Start.Line, context.Start.Column, context.GetText(), _level);
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

        return new MethodChainExpressionNode(null!, methodName, arguments, next, line, column, text, _level);
    }
}