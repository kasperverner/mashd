using Antlr4.Runtime.Tree;
using Mashd.Backend;

namespace TestProject1;

public class ExpressionUnitTests
{
    [Fact]
    public void CanParseSimpleNumber()
    {
        // Arrange
        string input = "42";
            
        // Act
        var result = ExpressionParser.Parse(input);
            
        // Assert
        Assert.NotNull(result);
        Assert.Equal(input, TestHelper.GetTerminalText(result));
    }
    
    [Fact]
    public void CanParseSimpleAddition()
    {
        // Arrange
        string input = "40+2";
            
        // Act
        var result = ExpressionParser.Parse(input);
            
        // Assert
        Assert.NotNull(result);
        Assert.Equal(input, TestHelper.GetTerminalText(result));
    }
    
    
}