using Antlr4.Runtime.Tree;
using Mashd.Backend;
using Xunit;

namespace TestProject1;

/*
 expression     : literal                                                   # LiteralExpression              - Done                   
                | ID                                                        # IdentifierExpression           - Done   
                | '(' expression ')'                                        # ParenExpression                - Done
                | functionCall                                              # FunctionCallExpression         - Done
                | expression '.' ID                                         # PropertyAccessExpression       - Keeps failing
                | expression '.' methodChain                                # MethodChainExpression          - Done
                | expression '++'                                           # PostIncrementExpression        - Done
                | expression '--'                                           # PostDecrementExpression        - Done
                | '++' expression                                           # PreIncrementExpression         - Done
                | '--' expression                                           # PreDecrementExpression         - Done
                | '-' expression                                            # NegationExpression             - Done
                | '!' expression                                            # NotExpression                  - Done
                | expression '*' expression                                 # MultiplicationExpression       - Done
                | expression '/' expression                                 # DivisionExpression             - Done
                | expression '%' expression                                 # ModuloExpression               - Done
                | expression '+' expression                                 # AdditionExpression             - Done
                | expression '-' expression                                 # SubtractionExpression          - Done
                | expression '<' expression                                 # LessThanExpression             - Done
                | expression '<=' expression                                # LessThanEqualExpression        - Done
                | expression '>' expression                                 # GreaterThanExpression          - Done
                | expression '>=' expression                                # GreaterThanEqualExpression     - Done
                | expression '==' expression                                # EqualityExpression             - Done
                | expression '!=' expression                                # InequalityExpression           - Done
                | expression '??' expression                                # NullishCoalescingExpression    - Done
                | expression '&&' expression                                # LogicalAndExpression           - Done
                | expression '||' expression                                # LogicalOrExpression            - Done
                | expression '?' expression ':' expression                  # TernaryExpression              - Done
                | '{' (keyValuePair (',' keyValuePair)*)? '}'               # ObjectExpression               - Done
*/

public class ExpressionUnitTests
{
    [Fact]
    public void CanParseSimpleNumber()
    {
        string input = "42";
        var result = ExpressionParser.Parse(input);
        Assert.NotNull(result);
        Assert.Equal(input, TestHelper.GetTerminalText(result));
    }

    [Fact]
    public void CanParseSimpleAddition()
    {
        string input = "40 + 2";
        var result = ExpressionParser.Parse(input);
        Assert.NotNull(result);
        Assert.Equal("40+2", TestHelper.GetTerminalText(result));
    }

    [Fact]
    public void CanParseNestedAdditionAndMultiplication()
    {
        string input = "2 + 3 * 4";
        var result = ExpressionParser.Parse(input);
        Assert.NotNull(result);
        Assert.Equal("2+3*4", TestHelper.GetTerminalText(result));
    }

    [Fact]
    public void CanParseParenthesizedExpression()
    {
        string input = "(1 + 2)";
        var result = ExpressionParser.Parse(input);
        Assert.NotNull(result);
        Assert.Equal("(1+2)", TestHelper.GetTerminalText(result));
    }

    [Fact]
    public void CanParseLogicalExpression()
    {
        string input = "true && false";
        var result = ExpressionParser.Parse(input);
        Assert.NotNull(result);
        Assert.Equal("true&&false", TestHelper.GetTerminalText(result));
    }

    [Fact]
    public void CanParseTernaryExpression()
    {
        string input = "x > 0 ? 1 : 0";
        var result = ExpressionParser.Parse(input);
        Assert.NotNull(result);
        Assert.Equal("x>0?1:0", TestHelper.GetTerminalText(result));
    }

    [Fact]
    public void CanParseFunctionCallExpression()
    {
        string input = "sum(1, 2)";
        var result = ExpressionParser.Parse(input);
        Assert.NotNull(result);
        Assert.Equal("sum(1,2)", TestHelper.GetTerminalText(result));
    }

    [Fact]
    public void CanParseObjectExpression()
    {
        string input = "{ key: 42 }";
        var result = ExpressionParser.Parse(input);
        Assert.NotNull(result);
        Assert.Equal("{key:42}", TestHelper.GetTerminalText(result));
    }

    [Fact]
    public void CanParseNegationExpression()
    {
        string input = "-5";
        var result = ExpressionParser.Parse(input);
        Assert.NotNull(result);
        Assert.Equal("-5", TestHelper.GetTerminalText(result));
    }

    [Fact]
    public void CanParsePostIncrementExpression()
    {
        string input = "x++";
        var result = ExpressionParser.Parse(input);
        Assert.NotNull(result);
        Assert.Equal("x++", TestHelper.GetTerminalText(result));
    }

    [Fact]
    public void ThrowsOnInvalidExpression_MissingOperand()
    {
        string input = "1 + ";
        Assert.Throws<ParseException>(() => ExpressionParser.Parse(input));
    }

    [Fact]
    public void ThrowsOnInvalidExpression_DoubleOperator()
    {
        string input = "x + + y";
        Assert.Throws<ParseException>(() => ExpressionParser.Parse(input));
    }

    [Fact]
    public void ThrowsOnInvalidExpression_TernaryMissingColon()
    {
        string input = "x > 0 ? 1";
        Assert.Throws<ParseException>(() => ExpressionParser.Parse(input));
    }

    [Fact]
    public void ThrowsOnInvalidExpression_ObjectMissingColon()
    {
        string input = "{ key 42 }";
        Assert.Throws<ParseException>(() => ExpressionParser.Parse(input));
    }

    [Fact]
    public void CanParseComplexChainedMethodCalls()
    {
        string input = "x.filter(y).map(z)";
        var result = ExpressionParser.Parse(input);
        Assert.NotNull(result);
        Assert.Equal("x.filter(y).map(z)", TestHelper.GetTerminalText(result));
    }
    
    [Fact]
    public void CanParsePropertyAccessExpression()
    {
        string input = "Text name = user.name;";
        var parser = TestHelper.CreateParser(input);
        var result = parser.statement();
        Assert.NotNull(result);

    }


[Fact]
public void CanParsePostDecrementExpression()
{
    string input = "x--";
    var result = ExpressionParser.Parse(input);
    Assert.NotNull(result);
    Assert.Equal("x--", TestHelper.GetTerminalText(result));
}

[Fact]
public void CanParsePreIncrementExpression()
{
    string input = "++x";
    var result = ExpressionParser.Parse(input);
    Assert.NotNull(result);
    Assert.Equal("++x", TestHelper.GetTerminalText(result));
}

[Fact]
public void CanParsePreDecrementExpression()
{
    string input = "--x";
    var result = ExpressionParser.Parse(input);
    Assert.NotNull(result);
    Assert.Equal("--x", TestHelper.GetTerminalText(result));
}

[Fact]
public void CanParseNotExpression()
{
    string input = "!false";
    var result = ExpressionParser.Parse(input);
    Assert.NotNull(result);
    Assert.Equal("!false", TestHelper.GetTerminalText(result));
}

[Fact]
public void CanParseDivisionExpression()
{
    string input = "10 / 2";
    var result = ExpressionParser.Parse(input);
    Assert.NotNull(result);
    Assert.Equal("10/2", TestHelper.GetTerminalText(result));
}

[Fact]
public void CanParseModuloExpression()
{
    string input = "10 % 3";
    var result = ExpressionParser.Parse(input);
    Assert.NotNull(result);
    Assert.Equal("10%3", TestHelper.GetTerminalText(result));
}

[Fact]
public void CanParseSubtractionExpression()
{
    string input = "10 - 5";
    var result = ExpressionParser.Parse(input);
    Assert.NotNull(result);
    Assert.Equal("10-5", TestHelper.GetTerminalText(result));
}

[Fact]
public void CanParseLessThanExpression()
{
    string input = "a < b";
    var result = ExpressionParser.Parse(input);
    Assert.NotNull(result);
    Assert.Equal("a<b", TestHelper.GetTerminalText(result));
}

[Fact]
public void CanParseLessThanOrEqualExpression()
{
    string input = "a <= b";
    var result = ExpressionParser.Parse(input);
    Assert.NotNull(result);
    Assert.Equal("a<=b", TestHelper.GetTerminalText(result));
}

[Fact]
public void CanParseGreaterThanOrEqualExpression()
{
    string input = "a >= b";
    var result = ExpressionParser.Parse(input);
    Assert.NotNull(result);
    Assert.Equal("a>=b", TestHelper.GetTerminalText(result));
}

[Fact]
public void CanParseInequalityExpression()
{
    string input = "a != b";
    var result = ExpressionParser.Parse(input);
    Assert.NotNull(result);
    Assert.Equal("a!=b", TestHelper.GetTerminalText(result));
}

[Fact]
public void CanParseNullishCoalescingExpression()
{
    string input = "x ?? y";
    var result = ExpressionParser.Parse(input);
    Assert.NotNull(result);
    Assert.Equal("x??y", TestHelper.GetTerminalText(result));
}

}
