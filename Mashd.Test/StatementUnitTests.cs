using Mashd.Backend;    
using Antlr4.Runtime;
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

        var result = new MashdParser(new CommonTokenStream(new MashdLexer(new AntlrInputStream(input)))).statement();

        Assert.NotNull(result);
        Assert.IsType<MashdParser.IfStatementContext>(result);
    }

    [Fact]
    public void CanParseVariableDeclaration()
    {
        var input = TestSnippets.VariableDeclaration;

        var result = new MashdParser(new CommonTokenStream(new MashdLexer(new AntlrInputStream(input)))).statement();;

        Assert.NotNull(result);
        Assert.IsType<MashdParser.VariableDeclarationContext>(result);
    }

    [Fact]
    public void CanParseAssignment()
    {
        var input = TestSnippets.Assignment;

        var result = new MashdParser(new CommonTokenStream(new MashdLexer(new AntlrInputStream(input)))).statement();;

        Assert.NotNull(result);
        Assert.IsType<MashdParser.AssignmentContext>(result);
    }

    [Fact]
    public void CanParseReturnStatement()
    {
        var input = TestSnippets.ReturnStatement;

        var result = new MashdParser(new CommonTokenStream(new MashdLexer(new AntlrInputStream(input)))).statement();;

        Assert.NotNull(result);
        Assert.IsType<MashdParser.ReturnStatementContext>(result);
    }
    
    [Fact]
    public void ThrowsOnInvalidAssignment()
    {
        // Arrange
        var input = "x + 5 = 10;"; // Invalid assignment

        // Act
        var parser = new MashdParser(new CommonTokenStream(new MashdLexer(new AntlrInputStream(input))));
        parser.AddErrorListener(new ThrowingErrorListener());
        parser.ErrorHandler = new BailErrorStrategy();
        
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
        var parser = new MashdParser(new CommonTokenStream(new MashdLexer(new AntlrInputStream(input))));
        parser.AddErrorListener(new ThrowingErrorListener());
        parser.ErrorHandler = new BailErrorStrategy();
        
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
        var parser = new MashdParser(new CommonTokenStream(new MashdLexer(new AntlrInputStream(input))));
        parser.AddErrorListener(new ThrowingErrorListener());
        parser.ErrorHandler = new BailErrorStrategy();
        
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
        var parser = new MashdParser(new CommonTokenStream(new MashdLexer(new AntlrInputStream(input))));
        parser.AddErrorListener(new ThrowingErrorListener());
        parser.ErrorHandler = new BailErrorStrategy();
        
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
        var parser = new MashdParser(new CommonTokenStream(new MashdLexer(new AntlrInputStream(input))));
        parser.AddErrorListener(new ThrowingErrorListener());
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
        var parser = new MashdParser(new CommonTokenStream(new MashdLexer(new AntlrInputStream(input))));
        parser.AddErrorListener(new ThrowingErrorListener());
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
        var parser = new MashdParser(new CommonTokenStream(new MashdLexer(new AntlrInputStream(input))));
        parser.AddErrorListener(new ThrowingErrorListener());
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
        var parser = new MashdParser(new CommonTokenStream(new MashdLexer(new AntlrInputStream(input))));
        parser.AddErrorListener(new ThrowingErrorListener());
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
        var parser = new MashdParser(new CommonTokenStream(new MashdLexer(new AntlrInputStream(input))));
        parser.AddErrorListener(new ThrowingErrorListener());
        parser.ErrorHandler = new BailErrorStrategy();
        
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
        var result = new MashdParser(new CommonTokenStream(new MashdLexer(new AntlrInputStream(input)))).statement();

        // Assert
        Assert.NotNull(result);
        Assert.IsType<MashdParser.IfStatementContext>(result);
    }
    
}