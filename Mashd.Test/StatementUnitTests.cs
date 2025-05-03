using Mashd.Backend;    
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Xunit.Abstractions;

namespace TestProject1;

public class StatementUnitTests
{
    [Fact]
    public void CanParseFunctionDeclaration()
    {
        // Arrange
        var input = TestSnippets.FunctionDeclaration;

        // Act
        var result = new MashdParser(new CommonTokenStream(new MashdLexer(new AntlrInputStream(input))))
            .definition(); // Function declarations are part of the 'definition' rule

        // Assert
        Assert.NotNull(result);
        Assert.IsType<MashdParser.FunctionDefinitionContext>(result);
    }

    [Fact]
    public void CanParseIfStatement()
    {
        var input = TestSnippets.IfStatement;

        var parser = TestHelper.CreateParser(input);
        var result = parser.statement();

        Assert.NotNull(result);
        Assert.IsType<MashdParser.IfStatementContext>(result);
    }

    [Fact]
    public void CanParseVariableDeclaration()
    {
        var input = TestSnippets.VariableDeclaration;

        var parser = TestHelper.CreateParser(input);
        var result = parser.statement();

        Assert.NotNull(result);
        Assert.IsType<MashdParser.VariableDeclarationContext>(result);
    }

    [Fact]
    public void CanParseAssignment()
    {
        var input = TestSnippets.Assignment;
        
        var parser = TestHelper.CreateParser(input);
        var result = parser.statement();

        Assert.NotNull(result);
        Assert.IsType<MashdParser.AssignmentContext>(result);
    }

    [Fact]
    public void CanParseReturnStatement()
    {
        var input = TestSnippets.ReturnStatement;
        
        var parser = TestHelper.CreateParser(input);
        var result = parser.statement();

        Assert.NotNull(result);
        Assert.IsType<MashdParser.ReturnStatementContext>(result);
    }
    
    [Fact]
    public void ThrowsOnInvalidAssignment()
    {
        // Arrange
        var input = "x + 5 = 10;"; // Invalid assignment

        // Act
        var parser = TestHelper.CreateParser(input);
        
        // Assert
        var exception = Assert.Throws<ParseException>(() =>
            parser.statement());

        Assert.NotNull(exception);
    }
    
    [Fact]
    public void ThrowsOnInvalidReturnStatement()
    {
        // Arrange
        var input = "return;";

        // Act
        var parser = TestHelper.CreateParser(input);;
        
        // Assert
        var exception = Assert.Throws<ParseException>(() =>
            parser.statement());

        Assert.NotNull(exception);
    }
    
    [Fact]
    public void ThrowsOnInvalidIfStatement()
    {
        // Arrange
        var input = "if (true) { return 1; } else;";

        // Act
        var parser = TestHelper.CreateParser(input);
        
        // Assert
        var exception = Assert.Throws<ParseException>(() =>
            parser.statement());

        Assert.NotNull(exception);
    }
    
    [Fact]
    public void ThrowsOnInvalidFunctionDeclaration()
    {
        // Arrange
        var input = "function Integer add(Integer a, Integer b) { return a + b; } 123;"; 

        // Act
        var parser = TestHelper.CreateParser(input);
        
        // Assert
        var exception = Assert.Throws<ParseException>(() =>
            parser.definition());
        
        Assert.NotNull(exception);
    }
    
    [Fact]
    public void ThrowsOnInvalidVariableDeclaration()
    {
        // Arrange
        var input = "integer x = ; 10;"; 

        // Act
        var parser = TestHelper.CreateParser(input);
        parser.ErrorHandler = new BailErrorStrategy();
        
        // Assert
        var exception = Assert.Throws<ParseException>(() =>
            parser.statement());
        
        Assert.NotNull(exception);
    }
    
    [Fact]
    public void ThrowsOnInvalidFunctionReturnType()
    {
        // Arrange
        var input = "123 add(Integer a, Integer b) { return a + b; }"; 

        // Act
        var parser = TestHelper.CreateParser(input);
        parser.ErrorHandler = new BailErrorStrategy();
        
        // Assert
        var exception = Assert.Throws<ParseException>(() =>
            parser.definition());
        
        Assert.NotNull(exception);
    }
    
    [Fact]
    public void CanParseEmptyFunctionBody()
    {
        // Arrange
        var input = "Integer add(Integer a, Integer b) {}"; 

        // Act
        var parser = TestHelper.CreateParser(input);
        parser.ErrorHandler = new BailErrorStrategy();
        
        // Assert
        var result = parser.definition();
        
        Assert.NotNull(result);
        Assert.IsType<MashdParser.FunctionDefinitionContext>(result);
    }
    
    [Fact]
    public void ThrowsOnInvalidNestedIfStatements()
    {
        // Arrange
        var input = "if (true) { if (false) { return 1; } else; }";

        // Act
        var parser = TestHelper.CreateParser(input);
        parser.ErrorHandler = new BailErrorStrategy();
        
        // Assert
        var exception = Assert.Throws<ParseException>(() =>
            parser.statement());

        Assert.NotNull(exception);
    }
    
    [Fact]
    public void CanParseVariableDeclarationComplexExpression()
    {
        // Arrange
        var input = "Decimal complex = x + y * (z - 5) / 2;";

        // Act
        var parser = TestHelper.CreateParser(input);
        
        // Assert
        var result = parser.statement();
        
        Assert.NotNull(result);
        Assert.IsType<MashdParser.VariableDeclarationContext>(result);
    }
    
    [Fact]
    public void CanParseNestedIfStatements()
    {
        // Arrange
        var input = "if (x > 5) { if (y < 10) { return y; } else { return x; } }";

        // Act
        var parser = TestHelper.CreateParser(input);
        var result = parser.statement();

        // Assert
        Assert.NotNull(result);
        Assert.IsType<MashdParser.IfStatementContext>(result);
    }
    
    [Fact]
    public void CanParseFunctionWithMultipleParameters()
    {
        // Arrange
        var input = "Integer add(Integer a, Integer b, Integer c) { return a + b + c; }";

        // Act
        var parser = TestHelper.CreateParser(input);
        var result = parser.definition();

        // Assert
        Assert.NotNull(result);
        Assert.IsType<MashdParser.FunctionDefinitionContext>(result);
    }
    
    [Fact]
    public void CanParseFunctionWithNoParameters()
    {
        // Arrange
        var input = "Integer getValue() { return 42; }";

        // Act
        var parser = TestHelper.CreateParser(input);
        var result = parser.definition();

        // Assert
        Assert.NotNull(result);
        Assert.IsType<MashdParser.FunctionDefinitionContext>(result);
    }
    
    [Fact]
    public void CanParseIfStatementWoElse()
    {
        // Arrange
        var input = "if (x > 5) { return x; }";

        // Act
        var parser = TestHelper.CreateParser(input);
        var result = parser.statement();

        // Assert
        Assert.NotNull(result);
        Assert.IsType<MashdParser.IfStatementContext>(result);
    }
    
    [Fact]
    public void EmptyBlockStatement()
    {
        // Arrange
        var input = "{}";

        // Act
        var parser = TestHelper.CreateParser(input);
        var result = parser.block();

        // Assert
        Assert.NotNull(result);
        Assert.IsType<MashdParser.BlockDefinitionContext>(result);
    }
    
    [Fact]
    public void MultipleStatementsInBlock()
    {
        // Arrange
        var input = "{ Integer x = 5; x = x + 1; return x; }";

        // Act
        var parser = TestHelper.CreateParser(input);
        var result = parser.block();

        // Assert
        Assert.NotNull(result);
        Assert.IsType<MashdParser.BlockDefinitionContext>(result);
    }
    
    [Fact]
    public void SingleLineIfStatement()
    {
        // Arrange
        var input = "if (x > 5) { return x; }";

        // Act
        var parser = TestHelper.CreateParser(input);
        var result = parser.statement();

        // Assert
        Assert.NotNull(result);
        Assert.IsType<MashdParser.IfStatementContext>(result);
    }
    
    [Fact]
    public void ReturnStatementWithExpression()
    {
        // Arrange
        var input = "return x + y;";

        // Act
        var parser = TestHelper.CreateParser(input);
        var result = parser.statement();

        // Assert
        Assert.NotNull(result);
        Assert.IsType<MashdParser.ReturnStatementContext>(result);
    }

    [Fact]
    public void MismatchedBracketStatement()
    {
        // Arrange
        var input = "if (x > 5) { return x; } else { return y;";

        // Act
        var parser = TestHelper.CreateParser(input);
        
        // Assert
        var exception = Assert.Throws<ParseCanceledException>(() =>
            parser.statement());

        Assert.NotNull(exception);
    }
    
    [Fact]
    public void IfStatementNoWoCondition()
    {
        // Arrange
        var input = "if { return x; }";

        // Act
        var parser = TestHelper.CreateParser(input);
        
        // Assert
        var exception = Assert.Throws<ParseCanceledException>(() =>
            parser.statement());

        Assert.NotNull(exception);
    }
    
    
}