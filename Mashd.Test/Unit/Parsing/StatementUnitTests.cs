using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Mashd.Backend;

namespace Mashd.Test;

public class StatementUnitTests
{
    [Theory]
    [InlineData("Boolean isEven(Integer x) { return x % 2 == 0; }", typeof(MashdParser.FunctionDefinitionContext))]
    [InlineData("Integer add(Integer a, Integer b, Integer c) { return a + b + c; }", typeof(MashdParser.FunctionDefinitionContext))]
    [InlineData("Integer getValue() { return 42; }", typeof(MashdParser.FunctionDefinitionContext))]
    [InlineData("Integer add(Integer a, Integer b) {}", typeof(MashdParser.FunctionDefinitionContext))]
    public void CanParseValidDefinitions(string code, Type expectedType)
    {
        var parser = TestHelper.CreateParser(code);
        var result = parser.definition();

        Assert.NotNull(result);
        Assert.IsType(expectedType, result);
    }

    [Theory]
    [InlineData("if (true) {return 1;}", typeof(MashdParser.IfStatementContext))]
    [InlineData("Integer x = 5;", typeof(MashdParser.VariableDeclarationContext))]
    [InlineData("x = 10;", typeof(MashdParser.AssignmentContext))]
    [InlineData("return 7;", typeof(MashdParser.ReturnStatementContext))]
    [InlineData("Decimal complex = x + y * (z - 5) / 2;", typeof(MashdParser.VariableDeclarationContext))]
    [InlineData("if (x > 5) { if (y < 10) { return y; } else { return x; } }", typeof(MashdParser.IfStatementContext))]
    [InlineData("if (x > 5) { return x; }", typeof(MashdParser.IfStatementContext))]
    [InlineData("return x + y;", typeof(MashdParser.ReturnStatementContext))]
    public void CanParseValidStatements(string code, Type expectedType)
    {
        var parser = TestHelper.CreateParser(code);
        var result = parser.statement();

        Assert.NotNull(result);
        Assert.IsType(expectedType, result);
    }
    
    [Theory]
    [InlineData("{}", typeof(MashdParser.BlockDefinitionContext))]
    [InlineData("{ Integer x = 5; x = x + 1; return x; }", typeof(MashdParser.BlockDefinitionContext))]
    public void CanParseValidBlocks(string code, Type expectedType)
    {
        var parser = TestHelper.CreateParser(code);
        var result = parser.block();

        Assert.NotNull(result);
        Assert.IsType(expectedType, result);
    }

    
    [Theory]
    [InlineData("x + 5 = 10;")]
    [InlineData("return;")]
    [InlineData("if (true) { return 1; } else;")]
    [InlineData("function Integer add(Integer a, Integer b) { return a + b; } 123;")]
    [InlineData("integer x = ; 10;")]
    [InlineData("123 add(Integer a, Integer b) { return a + b; }")]
    [InlineData("if (true) { if (false) { return 1; } else; }")]
    public void ThrowsOnInvalidStatements(string input)
    {
        var parser = TestHelper.CreateParser(input);
        Assert.Throws<ParseException>(() => parser.statement());
    }

    [Theory]
    [InlineData("if (x > 5) { return x; } else { return y;")]
    [InlineData("if { return x; }")]
    public void ThrowsParseCanceledExceptionOnCriticalSyntaxErrors(string input)
    {
        var parser = TestHelper.CreateParser(input);
        parser.ErrorHandler = new BailErrorStrategy();

        Assert.Throws<ParseCanceledException>(() => parser.statement());
    }
}
