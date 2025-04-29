using Mashd.Backend;

namespace TestProject1;

public class ImportUnitTests
{
    [Fact]
    public void CanParseImportStatement()
    {
        // Arrange
        string input = "import \"another_file.mashd\";";
        
        // Act
        var result = ExpressionParser.Parse(input);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(input, TestHelper.GetTerminalText(result));
    }
}