using Antlr4.Runtime;
using Mashd.Frontend;
using Mashd.Frontend.AST;
using Mashd.Frontend.AST.Definitions;
using Mashd.Frontend.AST.Expressions;
using Mashd.Frontend.AST.Statements;
using Moq;

namespace Mashd.Test.Unit.AST_Construction
{
    public class AstBuilderTests
    {
        private readonly Mock<ErrorReporter> _mockErrorReporter;
        private readonly AstBuilder _astBuilder;

        public AstBuilderTests()
        {
            _mockErrorReporter = new Mock<ErrorReporter>();
            _astBuilder = new AstBuilder(_mockErrorReporter.Object, 1);
        }

        private MashdParser CreateParser(string input)
        {
            var inputStream = new AntlrInputStream(input);
            var lexer = new MashdLexer(inputStream);
            var tokenStream = new CommonTokenStream(lexer);
            return new MashdParser(tokenStream);
        }

        [Fact]
        public void VisitProgram_WithImportAndDefinitionAndStatement_ShouldReturnCorrectProgramNode()
        {
            // Arrange
            var input = @"
                import ""module.mashd"";
                Integer add(Integer a, Integer b) { return a + b; }
                Integer result = add(1, 2);
            ";
            var parser = CreateParser(input);
            var programContext = parser.program();

            // Act
            var result = _astBuilder.VisitProgram(programContext) as ProgramNode;

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Imports);
            Assert.Single(result.Definitions);
            Assert.Single(result.Statements);

            Assert.Equal("module.mashd", result.Imports[0].Path);
            Assert.IsType<FunctionDefinitionNode>(result.Definitions[0]);
            Assert.IsType<VariableDeclarationNode>(result.Statements[0]);
        }
        
        [Fact]
        public void VisitImportDeclaration_WithValidPath_ShouldReturnImportNode()
        {
            // Arrange
            var input = @"import ""data/sample.mashd"";";
            var parser = CreateParser(input);
            var importContext = parser.importStatement();

            // Act
            var result = _astBuilder.Visit(importContext) as ImportNode;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("data/sample.mashd", result.Path);
        }
        
        [Fact]
        public void VisitFunctionDefinition_WithValidSignature_ShouldReturnFunctionDefinitionNode()
        {
            // Arrange
            var input = @"Integer add(Integer a, Integer b) { return a + b; }";
            var parser = CreateParser(input);
            var definitionContext = parser.definition();

            // Act
            var result = _astBuilder.Visit(definitionContext) as FunctionDefinitionNode;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("add", result.Identifier);
            Assert.Equal(SymbolType.Integer, result.DeclaredType);
            Assert.NotNull(result.ParameterList);
            Assert.Equal(2, result.ParameterList.Parameters.Count);
            Assert.Equal("a", result.ParameterList.Parameters[0].Identifier);
            Assert.Equal(SymbolType.Integer, result.ParameterList.Parameters[0].DeclaredType);
            Assert.Equal("b", result.ParameterList.Parameters[1].Identifier);
            Assert.Equal(SymbolType.Integer, result.ParameterList.Parameters[1].DeclaredType);
            Assert.NotNull(result.Body);
            Assert.Single(result.Body.Statements);
            Assert.IsType<ReturnNode>(result.Body.Statements[0]);
        }
        
        [Fact]
        public void VisitLiteralExpression_WithIntegerLiteral_ShouldReturnLiteralNode()
        {
            // Arrange
            var input = "42;";
            var parser = CreateParser(input);
            var expressionContext = parser.expression();

            // Act
            var result = _astBuilder.Visit(expressionContext) as LiteralNode;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(42L, result.Value);
            Assert.Equal(SymbolType.Integer, result.ParsedType);
        }
        
        [Fact]
        public void VisitLiteralExpression_WithTextLiteral_ShouldReturnLiteralNode()
        {
            // Arrange
            var input = @"""Hello, World!"";";
            var parser = CreateParser(input);
            var expressionContext = parser.expression();

            // Act
            var result = _astBuilder.Visit(expressionContext) as LiteralNode;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Hello, World!", result.Value);
            Assert.Equal(SymbolType.Text, result.ParsedType);
        }

        [Fact]
        public void VisitLiteralExpression_WithBooleanLiteral_ShouldReturnLiteralNode()
        {
            // Arrange
            var input = "true;";
            var parser = CreateParser(input);
            var expressionContext = parser.expression();

            // Act
            var result = _astBuilder.Visit(expressionContext) as LiteralNode;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(true, result.Value);
            Assert.Equal(SymbolType.Boolean, result.ParsedType);
        }
        
        [Fact]
        public void VisitBinaryExpression_WithAddOperator_ShouldReturnBinaryNode()
        {
            // Arrange
            var input = "a + b;";
            var parser = CreateParser(input);
            var expressionContext = parser.expression();

            // Act
            var result = _astBuilder.Visit(expressionContext) as BinaryNode;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(OpType.Add, result.Operator);
            Assert.IsType<IdentifierNode>(result.Left);
            Assert.IsType<IdentifierNode>(result.Right);
            Assert.Equal("a", (result.Left as IdentifierNode).Name);
            Assert.Equal("b", (result.Right as IdentifierNode).Name);
        }
        
        [Fact]
        public void VisitBinaryExpression_WithComparisonOperator_ShouldReturnBinaryNode()
        {
            // Arrange
            var input = "a > b;";
            var parser = CreateParser(input);
            var expressionContext = parser.expression();

            // Act
            var result = _astBuilder.Visit(expressionContext) as BinaryNode;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(OpType.GreaterThan, result.Operator);
            Assert.IsType<IdentifierNode>(result.Left);
            Assert.IsType<IdentifierNode>(result.Right);
            Assert.Equal("a", (result.Left as IdentifierNode).Name);
            Assert.Equal("b", (result.Right as IdentifierNode).Name);
        }
        
        [Fact]
        public void VisitUnaryExpression_WithNegationOperator_ShouldReturnUnaryNode()
        {
            // Arrange
            var input = "-x;";
            var parser = CreateParser(input);
            var expressionContext = parser.expression();

            // Act
            var result = _astBuilder.Visit(expressionContext) as UnaryNode;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(OpType.Negation, result.Operator);
            Assert.IsType<IdentifierNode>(result.Operand);
            Assert.Equal("x", (result.Operand as IdentifierNode).Name);
        }
        
        [Fact]
        public void VisitMethodCallExpression_WithSingleMethod_ShouldReturnMethodChainNode()
        {
            // Arrange
            var input = "myDataset.toTable(\"age\");";
            var parser = CreateParser(input);
            var expressionContext = parser.expression();

            // Act
            var result = _astBuilder.Visit(expressionContext) as MethodChainExpressionNode;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("toTable", result.MethodName);
            Assert.IsType<IdentifierNode>(result.Left);
            Assert.Equal("myDataset", ((IdentifierNode)result.Left).Name);
            Assert.Single(result.Arguments);
            Assert.Null(result.Next);
        }
        
        [Fact]
        public void VisitMethodCallExpression_WithChainedMethods_ShouldReturnMethodChainNode()
        {
            // Arrange
            var input = "myMashd.transform(\"coLA\");";
            var parser = CreateParser(input);
            var expressionContext = parser.expression();

            // Act
            var result = _astBuilder.Visit(expressionContext) as MethodChainExpressionNode;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("transform", result.MethodName);
            Assert.IsType<IdentifierNode>(result.Left);
            Assert.Equal("myMashd", ((IdentifierNode)result.Left).Name);
            Assert.Single(result.Arguments);
            Assert.Null(result.Next);
        }
        
        [Fact]
        public void VisitIfStatement_WithoutElse_ShouldReturnIfNode()
        {
            // Arrange
            var input = "if (x > 5) { y = 10; }";
            var parser = CreateParser(input);
            var statementContext = parser.statement();

            // Act
            var result = _astBuilder.Visit(statementContext) as IfNode;

            // Assert
            Assert.NotNull(result);
            Assert.IsType<BinaryNode>(result.Condition);
            Assert.NotNull(result.ThenBlock);
            Assert.Single(result.ThenBlock.Statements);
            Assert.False(result.HasElse);
            Assert.Null(result.ElseBlock);
        }
        
        [Fact]
        public void VisitIfStatement_WithElse_ShouldReturnIfNode()
        {
            // Arrange
            var input = "if (x > 5) { y = 10; } else { y = 5; }";
            var parser = CreateParser(input);
            var statementContext = parser.statement();

            // Act
            var result = _astBuilder.Visit(statementContext) as IfNode;

            // Assert
            Assert.NotNull(result);
            Assert.IsType<BinaryNode>(result.Condition);
            Assert.NotNull(result.ThenBlock);
            Assert.Single(result.ThenBlock.Statements);
            Assert.True(result.HasElse);
            Assert.NotNull(result.ElseBlock);
            Assert.Single(result.ElseBlock.Statements);
        }
        
        [Fact]
        public void VisitIfStatement_WithElseIf_ShouldReturnIfNodeWithNestedIf()
        {
            // Arrange
            var input = "if (x > 10) { y = 20; } else if (x > 5) { y = 10; }";
            var parser = CreateParser(input);
            var statementContext = parser.statement();

            // Act
            var result = _astBuilder.Visit(statementContext) as IfNode;

            // Assert
            Assert.NotNull(result);
            Assert.IsType<BinaryNode>(result.Condition);
            Assert.NotNull(result.ThenBlock);
            Assert.Single(result.ThenBlock.Statements);
            Assert.True(result.HasElse);
            Assert.NotNull(result.ElseBlock);
            Assert.Single(result.ElseBlock.Statements);
            Assert.IsType<IfNode>(result.ElseBlock.Statements[0]);
        }
        
        [Fact]
        public void VisitVariableDeclaration_WithoutInitialization_ShouldReturnVariableDeclarationNode()
        {
            // Arrange
            var input = "Integer count;";
            var parser = CreateParser(input);
            var statementContext = parser.statement();

            // Act
            var result = _astBuilder.Visit(statementContext) as VariableDeclarationNode;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(SymbolType.Integer, result.DeclaredType);
            Assert.Equal("count", result.Identifier);
            Assert.Null(result.Expression);
        }
        
        [Fact]
        public void VisitVariableDeclaration_WithInitialization_ShouldReturnVariableDeclarationNode()
        {
            // Arrange
            var input = "Integer count = 0;";
            var parser = CreateParser(input);
            var statementContext = parser.statement();

            // Act
            var result = _astBuilder.Visit(statementContext) as VariableDeclarationNode;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(SymbolType.Integer, result.DeclaredType);
            Assert.Equal("count", result.Identifier);
            Assert.NotNull(result.Expression);
            Assert.IsType<LiteralNode>(result.Expression);
        }
        
        [Fact]
        public void VisitAssignment_WithValidExpression_ShouldReturnAssignmentNode()
        {
            // Arrange
            var input = "count = count + 1;";
            var parser = CreateParser(input);
            var statementContext = parser.statement();

            // Act
            var result = _astBuilder.Visit(statementContext) as AssignmentNode;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("count", result.Identifier);
            Assert.NotNull(result.Expression);
            Assert.IsType<BinaryNode>(result.Expression);
        }
        
        [Fact]
        public void VisitTernaryExpression_ShouldReturnTernaryNode()
        {
            // Arrange
            var input = "isValid ? \"Valid\" : \"Invalid\";";
            var parser = CreateParser(input);
            var expressionContext = parser.expression();

            // Act
            var result = _astBuilder.Visit(expressionContext) as TernaryNode;

            // Assert
            Assert.NotNull(result);
            Assert.IsType<IdentifierNode>(result.Condition);
            Assert.Equal("isValid", (result.Condition as IdentifierNode).Name);
            Assert.IsType<LiteralNode>(result.TrueExpression);
            Assert.Equal("Valid", (result.TrueExpression as LiteralNode).Value);
            Assert.IsType<LiteralNode>(result.FalseExpression);
            Assert.Equal("Invalid", (result.FalseExpression as LiteralNode).Value);
        }
        
        [Fact]
        public void VisitNullishCoalescingExpression_ShouldReturnBinaryNode()
        {
            // Arrange
            var input = "firstName ?? \"Unknown\";";
            var parser = CreateParser(input);
            var expressionContext = parser.expression();

            // Act
            var result = _astBuilder.Visit(expressionContext) as BinaryNode;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(OpType.NullishCoalescing, result.Operator);
            Assert.IsType<IdentifierNode>(result.Left);
            Assert.Equal("firstName", (result.Left as IdentifierNode).Name);
            Assert.IsType<LiteralNode>(result.Right);
            Assert.Equal("Unknown", (result.Right as LiteralNode).Value);
        }
  
        [Fact]
        public void VisitReturnStatement_WithExpression_ShouldReturnReturnNode()
        {
            // Arrange
            var input = "return x * 2;";
            var parser = CreateParser(input);
            var statementContext = parser.statement();

            // Act
            var result = _astBuilder.Visit(statementContext) as ReturnNode;

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Expression);
            Assert.IsType<BinaryNode>(result.Expression);
        }
        
        [Fact]
        public void VisitExpression_WithNestedExpressions_ShouldPreserveOperatorPrecedence()
        {
            // Arrange
            var input = "a + b * c;";
            var parser = CreateParser(input);
            var expressionContext = parser.expression();

            // Act
            var result = _astBuilder.Visit(expressionContext) as BinaryNode;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(OpType.Add, result.Operator);
            Assert.IsType<IdentifierNode>(result.Left);
            Assert.IsType<BinaryNode>(result.Right);
            
            var rightNode = result.Right as BinaryNode;
            Assert.Equal(OpType.Multiply, rightNode.Operator);
        }
        
        [Fact]
        public void VisitParenExpression_ShouldOverrideNormalPrecedence()
        {
            // Arrange
            var input = "(a + b) * c;";
            var parser = CreateParser(input);
            var expressionContext = parser.expression();

            // Act
            var result = _astBuilder.Visit(expressionContext) as BinaryNode;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(OpType.Multiply, result.Operator);
            Assert.IsType<ParenNode>(result.Left);
            Assert.IsType<IdentifierNode>(result.Right);
            
            var parenNode = result.Left as ParenNode;
            Assert.IsType<BinaryNode>(parenNode.InnerExpression);
            var innerBinary = parenNode.InnerExpression as BinaryNode;
            Assert.Equal(OpType.Add, innerBinary.Operator);
        }
        
        [Fact]
        public void VisitExpressionStatement_ShouldReturnExpressionStatementNode()
        {
            // Arrange
            var input = "calculateTotal();";
            var parser = CreateParser(input);
            var statementContext = parser.statement();

            // Act
            var result = _astBuilder.Visit(statementContext) as ExpressionStatementNode;

            // Assert
            Assert.NotNull(result);
            Assert.IsType<FunctionCallNode>(result.Expression);
        }
    }
}