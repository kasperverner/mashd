using Antlr4.Runtime.Tree;
using Mashd.Backend;
using Xunit;

namespace TestProject1;

public class ExpressionUnitTests
{
    [Theory]
    [InlineData("42", "42", typeof(MashdParser.LiteralExpressionContext))]
    [InlineData("true", "true", typeof(MashdParser.LiteralExpressionContext))]
    [InlineData("x", "x", typeof(MashdParser.IdentifierExpressionContext))]
    [InlineData("(1 + 2)", "(1+2)", typeof(MashdParser.ParenExpressionContext))]
    [InlineData("sum(1, 2)", "sum(1,2)", typeof(MashdParser.FunctionCallExpressionContext))]
    [InlineData("user.firstName", "user.firstName", typeof(MashdParser.PropertyAccessExpressionContext))]
    [InlineData("x.filter(y).map(z)", "x.filter(y).map(z)", typeof(MashdParser.MethodChainExpressionContext))]
    [InlineData("x++", "x++", typeof(MashdParser.PostIncrementExpressionContext))]
    [InlineData("x--", "x--", typeof(MashdParser.PostDecrementExpressionContext))]
    [InlineData("++x", "++x", typeof(MashdParser.PreIncrementExpressionContext))]
    [InlineData("--x", "--x", typeof(MashdParser.PreDecrementExpressionContext))]
    [InlineData("-5", "-5", typeof(MashdParser.NegationExpressionContext))]
    [InlineData("!false", "!false", typeof(MashdParser.NotExpressionContext))]
    [InlineData("2 * 3", "2*3", typeof(MashdParser.MultiplicationExpressionContext))]
    [InlineData("10 / 2", "10/2", typeof(MashdParser.DivisionExpressionContext))]
    [InlineData("10 % 3", "10%3", typeof(MashdParser.ModuloExpressionContext))]
    [InlineData("10 + 5", "10+5", typeof(MashdParser.AdditionExpressionContext))]
    [InlineData("10 - 5", "10-5", typeof(MashdParser.SubtractionExpressionContext))]
    [InlineData("a < b", "a<b", typeof(MashdParser.LessThanExpressionContext))]
    [InlineData("a <= b", "a<=b", typeof(MashdParser.LessThanEqualExpressionContext))]
    [InlineData("a > b", "a>b", typeof(MashdParser.GreaterThanExpressionContext))]
    [InlineData("a >= b", "a>=b", typeof(MashdParser.GreaterThanEqualExpressionContext))]
    [InlineData("a == b", "a==b", typeof(MashdParser.EqualityExpressionContext))]
    [InlineData("a != b", "a!=b", typeof(MashdParser.InequalityExpressionContext))]
    [InlineData("x ?? y", "x??y", typeof(MashdParser.NullishCoalescingExpressionContext))]
    [InlineData("true && false", "true&&false", typeof(MashdParser.LogicalAndExpressionContext))]
    [InlineData("true || false", "true||false", typeof(MashdParser.LogicalOrExpressionContext))]
    [InlineData("x > 0 ? 1 : 0", "x>0?1:0", typeof(MashdParser.TernaryExpressionContext))]
    [InlineData("{ key: 42 }", "{key:42}", typeof(MashdParser.ObjectExpressionContext))]
    public void CanParseValidExpressions(string input, string expected, Type expectedType)
    {
        {
            var result = ExpressionParser.Parse(input);
            Assert.NotNull(result);
            Assert.Equal(expected, TestHelper.GetTerminalText(result));
            Assert.IsType(expectedType, result);
        }
    }

    [Theory]
    [InlineData("1 + ")]
    [InlineData("x + + y")]
    [InlineData("x > 0 ? 1")]
    [InlineData("{ key 42 }")]
    [InlineData("sum(1, 2")]
    [InlineData("(1 + 2")]
    [InlineData("x ??? y")]
    [InlineData("true &&")]
    [InlineData("x > 0 ? : 1")]
    public void ThrowsOnInvalidExpressions(string input)
    {
        Assert.Throws<ParseException>(() => ExpressionParser.Parse(input));
    }
}
