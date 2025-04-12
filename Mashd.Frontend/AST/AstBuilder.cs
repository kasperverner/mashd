using Mashd.Frontend.AST.Definitions;
using Mashd.Frontend.AST.Statements;

namespace Mashd.Frontend.AST;

using Antlr4.Runtime;
using Mashd.Frontend.AST.Expressions;
using Mashd.Frontend.AST.Statements;
using Mashd.Frontend.AST.Definitions;

public class AstBuilder : MashdBaseVisitor<AstNode>
{
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

    // Import Nodes

    // Definition Nodes

    public override AstNode VisitVariableDefinition(MashdParser.VariableDefinitionContext context)
    {
        string name = context.ID().GetText();
        string typeText = context.type().GetText();
        VarType type = ParseVariableType(typeText);

        var (line, column, text) = ExtractNodeInfo(context);

        return new VariableDefinitionNode(name, type, line, column, text);
    }


    // Expression Nodes

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

    // Unary Operation Nodes

    public override AstNode VisitNegationExpression(MashdParser.NegationExpressionContext context)
    {
        return CreateUnaryOperationNode(context.expression(), OpType.Negation, context);
    }

    public override AstNode VisitNotExpression(MashdParser.NotExpressionContext context)
    {
        return CreateUnaryOperationNode(context.expression(), OpType.Not, context);
    }

    public override AstNode VisitPostIncrementExpression(MashdParser.PostIncrementExpressionContext context)
    {
        return CreateUnaryOperationNode(context.expression(), OpType.PostIncrement, context);
    }

    public override AstNode VisitPostDecrementExpression(MashdParser.PostDecrementExpressionContext context)
    {
        return CreateUnaryOperationNode(context.expression(), OpType.PostDecrement, context);
    }

    public override AstNode VisitPreIncrementExpression(MashdParser.PreIncrementExpressionContext context)
    {
        return CreateUnaryOperationNode(context.expression(), OpType.PreIncrement, context);
    }

    public override AstNode VisitPreDecrementExpression(MashdParser.PreDecrementExpressionContext context)
    {
        return CreateUnaryOperationNode(context.expression(), OpType.PreDecrement, context);
    }


    // Binary Operation Nodes

    public override AstNode VisitAdditionExpression(MashdParser.AdditionExpressionContext context)
    {
        return CreateBinaryOperationNode(context.expression(0), context.expression(1), OpType.Add, context);
    }

    public override AstNode VisitSubtractionExpression(MashdParser.SubtractionExpressionContext context)
    {
        return CreateBinaryOperationNode(context.expression(0), context.expression(1), OpType.Subtract, context);
    }

    public override AstNode VisitMultiplicationExpression(MashdParser.MultiplicationExpressionContext context)
    {
        return CreateBinaryOperationNode(context.expression(0), context.expression(1), OpType.Multiply, context);
    }

    public override AstNode VisitDivisionExpression(MashdParser.DivisionExpressionContext context)
    {
        return CreateBinaryOperationNode(context.expression(0), context.expression(1), OpType.Divide, context);
    }

    public override AstNode VisitModuloExpression(MashdParser.ModuloExpressionContext context)
    {
        return CreateBinaryOperationNode(context.expression(0), context.expression(1), OpType.Modulo, context);
    }

    public override AstNode VisitEqualityExpression(MashdParser.EqualityExpressionContext context)
    {
        return CreateBinaryOperationNode(context.expression(0), context.expression(1), OpType.Equality, context);
    }

    public override AstNode VisitInequalityExpression(MashdParser.InequalityExpressionContext context)
    {
        return CreateBinaryOperationNode(context.expression(0), context.expression(1), OpType.Inequality, context);
    }

    public override AstNode VisitLessThanExpression(MashdParser.LessThanExpressionContext context)
    {
        return CreateBinaryOperationNode(context.expression(0), context.expression(1), OpType.LessThan, context);
    }

    public override AstNode VisitLessThanEqualExpression(MashdParser.LessThanEqualExpressionContext context)
    {
        return CreateBinaryOperationNode(context.expression(0), context.expression(1), OpType.LessThanEqual, context);
    }

    public override AstNode VisitGreaterThanExpression(MashdParser.GreaterThanExpressionContext context)
    {
        return CreateBinaryOperationNode(context.expression(0), context.expression(1), OpType.GreaterThan, context);
    }

    public override AstNode VisitGreaterThanEqualExpression(MashdParser.GreaterThanEqualExpressionContext context)
    {
        return CreateBinaryOperationNode(context.expression(0), context.expression(1), OpType.GreaterThanEqual,
            context);
    }

    public override AstNode VisitLogicalOrExpression(MashdParser.LogicalOrExpressionContext context)
    {
        return CreateBinaryOperationNode(context.expression(0), context.expression(1), OpType.LogicalOr, context);
    }

    public override AstNode VisitLogicalAndExpression(MashdParser.LogicalAndExpressionContext context)
    {
        return CreateBinaryOperationNode(context.expression(0), context.expression(1), OpType.LogicalAnd, context);
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

    public override AstNode VisitIntegerLiteral(MashdParser.IntegerLiteralContext context)
    {
        var (line, column, text) = ExtractNodeInfo(context);

        string intText = context.INTEGER().GetText();

        int value = int.Parse(intText);
        return new LiteralNode(value, line, column, text);
    }


    public override AstNode VisitDecimalLiteral(MashdParser.DecimalLiteralContext context)
    {
        var (line, column, text) = ExtractNodeInfo(context);

        string decText = context.DECIMAL().GetText();

        decimal value = decimal.Parse(decText);
        return new LiteralNode(value, line, column, text);
    }


    public override AstNode VisitTextLiteral(MashdParser.TextLiteralContext context)
    {
        // Remove surrounding quotes
        string rawText = context.TEXT().GetText();
        string value = rawText.Trim('"');
        var (line, column, text) = ExtractNodeInfo(context);

        return new LiteralNode(value, line, column, text);
    }

    public override AstNode VisitBooleanLiteral(MashdParser.BooleanLiteralContext context)
    {
        var (line, column, text) = ExtractNodeInfo(context);

        string boolText = context.BOOLEAN().GetText();

        bool value = boolText.Equals("true", StringComparison.OrdinalIgnoreCase);
        return new LiteralNode(value, line, column, text);
    }

    public override AstNode VisitDateLiteral(MashdParser.DateLiteralContext context)
    {
        var (line, column, text) = ExtractNodeInfo(context);

        string dateText = context.DATE().GetText();

        DateTime value = DateTime.Parse(dateText);
        return new LiteralNode(value, line, column, text);
    }

    public override AstNode VisitNullLiteral(MashdParser.NullLiteralContext context)
    {
        var (line, column, text) = ExtractNodeInfo(context);

        return new LiteralNode(null, line, column, text);
    }

    //public override AstNode VisitMashdLiteral(MashdParser.MashdLiteralContext context)
    // public override AstNode VisitSchemaLiteral(MashdParser.SchemaLiteralContext context)
    //public override AstNode VisitDatasetLiteral(MashdParser.DatasetLiteralContext context)


    // Statement Nodes

    public override AstNode VisitVariableDeclaration(MashdParser.VariableDeclarationContext context)
    {
        string typeText = context.type().GetText();
        VarType type = ParseVariableType(typeText);

        var identifier = context.ID().GetText();

        // Check if there is an initialization expression
        bool hasInitialization = context.expression() != null;

        // If there is an initialization expression visit it
        var expression = hasInitialization ? Visit(context.expression()) as ExpressionNode : null;

        return new VariableDeclarationNode(type, identifier, expression, hasInitialization, context.Start.Line, context.Start.Column, context.GetText());
    }
    
    public override AstNode VisitAssignment(MashdParser.AssignmentContext context)
    {
        var (line, column, text) = ExtractNodeInfo(context);

        string name = context.ID().GetText();
        var expression = Visit(context.expression()) as ExpressionNode;

        return new AssignmentNode(name, expression, line, column, text);
    }
    
    // Coalescing Assignment Nodes
    public override AstNode VisitAddAssignment(MashdParser.AddAssignmentContext context)
    {
        var (line, column, text) = ExtractNodeInfo(context);
        string identifier = context.ID().GetText();
        var expression = Visit(context.expression()) as ExpressionNode;
        return new CompoundAssignmentNode(identifier, OpType.Add, expression, line, column, text);
    }

    public override AstNode VisitSubtractAssignment(MashdParser.SubtractAssignmentContext context)
    {
        var (line, column, text) = ExtractNodeInfo(context);
        string identifier = context.ID().GetText();
        var expression = Visit(context.expression()) as ExpressionNode;
        return new CompoundAssignmentNode(identifier, OpType.Subtract, expression, line, column, text);
    }

    public override AstNode VisitMultiplyAssignment(MashdParser.MultiplyAssignmentContext context)
    {
        var (line, column, text) = ExtractNodeInfo(context);
        string identifier = context.ID().GetText();
        var expression = Visit(context.expression()) as ExpressionNode;
        return new CompoundAssignmentNode(identifier, OpType.Multiply, expression, line, column, text);
    }

    public override AstNode VisitDivisionAssignment(MashdParser.DivisionAssignmentContext context)
    {
        var (line, column, text) = ExtractNodeInfo(context);
        string identifier = context.ID().GetText();
        var expression = Visit(context.expression()) as ExpressionNode;
        return new CompoundAssignmentNode(identifier, OpType.Divide, expression, line, column, text);
    }

    public override AstNode VisitNullCoalescingAssignment(MashdParser.NullCoalescingAssignmentContext context)
    {
        var (line, column, text) = ExtractNodeInfo(context);
        string identifier = context.ID().GetText();
        var expression = Visit(context.expression()) as ExpressionNode;
        return new CompoundAssignmentNode(identifier, OpType.NullishCoalescing, expression, line, column, text);
    }


    // Helper Methods
    private (int line, int column, string text) ExtractNodeInfo(ParserRuleContext context)
    {
        return (context.Start.Line, context.Start.Column, context.GetText());
    }

    private VarType ParseVariableType(string typeText)
    {
        return typeText switch
        {
            "Boolean" => VarType.Boolean,
            "Integer" => VarType.Integer,
            "Decimal" => VarType.Decimal,
            "Text" => VarType.Text,
            "Mashd" => VarType.Mashd,
            "Date" => VarType.Date,
            "Dataset" => VarType.Dataset,
            "Schema" => VarType.Schema,
            _ => VarType.Unknown
        };
    }

    private AstNode CreateUnaryOperationNode(ParserRuleContext expressionContext, OpType operatorType,
        ParserRuleContext context)
    {
        var expression = Visit(expressionContext) as ExpressionNode;

        // Create and return a UnaryOperationNode
        return new UnaryNode(expression, operatorType, context.Start.Line, context.Start.Column, context.GetText());
    }

    private AstNode CreateBinaryOperationNode(ParserRuleContext leftContext, ParserRuleContext rightContext, OpType op,
        ParserRuleContext context)
    {
        // Visit the left operand
        var left = Visit(leftContext) as ExpressionNode;

        // Visit the right operand
        var right = Visit(rightContext) as ExpressionNode;

        // Create and return the BinaryOperationNode
        return new BinaryNode(left, right, op, context.Start.Line, context.Start.Column, context.GetText());
    }
}