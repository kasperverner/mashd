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
        
        var parameters = Visit (context.formalParameters()) as FormalParameterListNode;
        
        var body = Visit (context.block()) as BlockNode;
        
        return new FunctionDefinitionNode(functionName, returnType, parameters, body, line, column, text);
    }

    public override AstNode VisitSchemaDefinition(MashdParser.SchemaDefinitionContext context)
    {
        var identifier = context.ID().GetText();
        
        var schemaObject = Visit(context.schemaObject()) as SchemaObjectNode;
        
        var (line, column, text) = ExtractNodeInfo(context);
        
        return new SchemaDefinitionNode(identifier, schemaObject, line, column, text);    
    }

    public override AstNode VisitDatasetDefinition(MashdParser.DatasetDefinitionContext context)
    {
        var identifier = context.ID().GetText();
        
        var datasetObject = Visit(context.datasetObject()) as DatasetObjectNode;
        
        var (line, column, text) = ExtractNodeInfo(context);
        
        return new DatasetDefinitionNode(identifier, datasetObject, line, column, text);
    }
    
    public override AstNode VisitMashdDefinition(MashdParser.MashdDefinitionContext context)
    {
        var identifier = context.ID().GetText();
        
        var left = Visit(context.datasetObject(0)) as DatasetObjectNode;
        var right = Visit(context.datasetObject(1)) as DatasetObjectNode;

        var (line, column, text) = ExtractNodeInfo(context);
        
        return new MashdDefinitionNode(
            identifier,
            left,
            right,
            line,
            column,
            text);
    }


    // Expression Nodes
    
    public override AstNode VisitDatasetObjectExpression(MashdParser.DatasetObjectExpressionContext context)
    {
        var properties = new Dictionary<string, DatasetObjectNode.DatasetProperty>();

        if (context.datasetProperties() != null)
        {
            foreach (var child in context.datasetProperties().children)
            {
                if (child is MashdParser.DatasetAdapterContext adapterCtx)
                {
                    properties["adapter"] = new DatasetObjectNode.DatasetProperty("adapter", adapterCtx.TEXT().GetText().Trim('"'));
                }
                else if (child is MashdParser.DatasetSourceContext sourceCtx)
                {
                    properties["source"] = new DatasetObjectNode.DatasetProperty("source", sourceCtx.TEXT().GetText().Trim('"'));
                }
                else if (child is MashdParser.DatasetSchemaContext schemaCtx)
                {
                    properties["schema"] = new DatasetObjectNode.DatasetProperty("schema", schemaCtx.ID().GetText());
                }
                else if (child is MashdParser.CsvDelimiterContext delimCtx)
                {
                    properties["delimiter"] = new DatasetObjectNode.DatasetProperty("delimiter", delimCtx.TEXT().GetText().Trim('"'));
                }
                else if (child is MashdParser.DatabaseQueryContext queryCtx)
                {
                    properties["query"] = new DatasetObjectNode.DatasetProperty("query", queryCtx.TEXT().GetText().Trim('"'));
                }
                else if (child is MashdParser.DatasetSkipContext skipCtx)
                {
                    int skipValue = int.Parse(skipCtx.INTEGER().GetText());
                    properties["skip"] = new DatasetObjectNode.DatasetProperty("skip", skipValue);
                }
            }
        }
        
        return new DatasetObjectNode(
            context.Start.Line,
            context.Start.Column,
            context.GetText(),
            properties
        );
    }
    
    public override AstNode VisitSchemaObject(MashdParser.SchemaObjectContext context)
    {
        var fields = new Dictionary<string, SchemaField>();

        if (context.schemaProperties() != null)
        {
            foreach (var property in context.schemaProperties().schemaProperty())
            {
                var fieldName = property.ID().GetText();
                string fieldType = null;
                string fieldDisplayName = null;

                foreach (var fieldProp in property.schemaFieldProperty())
                {
                    if (fieldProp.type() != null)
                    {
                        fieldType = fieldProp.type().GetText();
                    }
                    else if (fieldProp.TEXT() != null)
                    {
                        fieldDisplayName = fieldProp.TEXT().GetText().Trim('"');
                    }
                }

                if (fieldType == null)
                {
                    errorReporter.Report.AstBuilder(property, $"Schema field '{fieldName}' is missing a type.");
                }

                fields[fieldName] = new SchemaField(fieldType, fieldDisplayName);
            }
        }

        var (line, column, text) = ExtractNodeInfo(context);

        return new SchemaObjectNode(fields, line, column, text);

    }

    public override ExpressionNode VisitMethodChainExpression(MashdParser.MethodChainExpressionContext context)
    {
        var target = Visit(context.expression())! as ExpressionNode;
        var methodChain = BuildMethodChain(context.methodChain());

        var (line, column, text) = ExtractNodeInfo(context);
        return new MethodChainExpressionNode(target, methodChain.MethodName, methodChain.Arguments, methodChain.Next, line, column, text);
    }

    public override AstNode VisitObjectExpression(MashdParser.ObjectExpressionContext context)
    {
        var (line, column, text) = ExtractNodeInfo(context);
        var pairs = new List<ObjectExpressionNode.KeyValuePair>();

        foreach (var pairCtx in context.keyValuePair())
        {
            var key = pairCtx.ID().GetText();
            var value = Visit(pairCtx.expression()) as ExpressionNode;
            pairs.Add(new ObjectExpressionNode.KeyValuePair(key, value));
        }

        return new ObjectExpressionNode(pairs, line, column, text);
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
    /*
    public override AstNode VisitDatasetLiteral(MashdParser.DatasetLiteralContext context)
    {
        var (line, column, text) = ExtractNodeInfo(context);
        var datasetText = context.DATASET().GetText();

        var content = datasetText
            .Replace("Dataset", "")
            .Trim()
            .TrimStart('{')
            .TrimEnd('}');

        var properties = new Dictionary<string, DatasetLiteralNode.DatasetProperty>();

        var lines = content.Split(new[] { '\n', '\r', ';' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var lineText in lines)
        {
            var entry = lineText.Trim();
            if (string.IsNullOrWhiteSpace(entry)) continue;

            var parts = entry.Split(new[] { ':' }, 2);
            if (parts.Length != 2) continue;

            var key = parts[0].Trim();
            var rawValue = parts[1].Trim().Trim('"');

            properties[key] = new DatasetLiteralNode.DatasetProperty(key, rawValue);
        }

        return new DatasetLiteralNode(line, column, text, properties);
    }
    */



    public override AstNode VisitLiteralExpression(MashdParser.LiteralExpressionContext context)
    {
        return Visit(context.literal());
    }

    public override AstNode VisitIntegerLiteral(MashdParser.IntegerLiteralContext context)
    {
        var (line, column, text) = ExtractNodeInfo(context);

        string intText = context.INTEGER().GetText();
        
        int value = int.Parse(intText);
        return new LiteralNode(value, line, column, text, SymbolType.Integer);
    }


    public override AstNode VisitDecimalLiteral(MashdParser.DecimalLiteralContext context)
    {
        var (line, column, text) = ExtractNodeInfo(context);

        string decText = context.DECIMAL().GetText();

        decimal value = decimal.Parse(decText);
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

    //public override AstNode VisitMashdLiteral(MashdParser.MashdLiteralContext context)


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

}