using Mashd.Backend;

namespace TestProject1;

public class StatementUnitTests
{
    [Fact]
    public void CanParseFunctionDeclaration()
    {
        // Arrange
        string input = "Boolean isEven(int x) { return x % 2 == 0; }";
        
        // Act
        var result = ExpressionParser.Parse(input);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(input, TestHelper.GetTerminalText(result));
    }
}