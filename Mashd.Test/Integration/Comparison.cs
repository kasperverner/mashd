namespace TestProject1.Integration;

public class Comparison
{
    [Theory]
    // less than / greater than
    [InlineData("1 < 2", true)]
    [InlineData("2 < 1", false)]
    [InlineData("2 > 1", true)]
    [InlineData("1 > 2", false)]
    // <= and >=
    [InlineData("1 <= 1", true)]
    [InlineData("1 <= 2", true)]
    [InlineData("2 >= 1", true)]
    [InlineData("1 >= 2", false)]
    // equality / inequality
    [InlineData("3 == 3", true)]
    [InlineData("3 == 4", false)]
    [InlineData("3 != 4", true)]
    [InlineData("3 != 3", false)]
    public void IntegerComparisons(string expr, bool expected)
    {
        // Arrange
        string source = $"Boolean test = {expr};";
        // Act
        var (interp, ast) = TestPipeline.Run(source);
        bool actual = TestPipeline.GetBoolean(interp, ast, "test");
        // Assert
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("3.5 < 5.0", true)]
    [InlineData("5.0 <= 5.0", true)]
    [InlineData("10.0 > 2.5", true)]
    [InlineData("2.0 >= 3.0", false)]
    [InlineData("4.0 == 4.0", true)]
    [InlineData("4.0 != 4.0", false)]
    public void DecimalComparisons(string expr, bool expected)
    {
        // Arrange
        string source = $"Boolean test = {expr};";
        // Act
        var (interp, ast) = TestPipeline.Run(source);
        bool actual = TestPipeline.GetBoolean(interp, ast, "test");
        // Assert
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("\"a\" == \"a\"", true)]
    [InlineData("\"a\" != \"b\"", true)]
    [InlineData("\"a\" != \"a\"", false)]
    [InlineData("\"a\" == \"b\"", false)]
    public void TextComparisons(string expr, bool expected)
    {
        // Arrange
        string source = $"Boolean test = {expr};";
        // Act
        var (interp, ast) = TestPipeline.Run(source);
        bool actual = TestPipeline.GetBoolean(interp, ast, "test");
        // Assert
        Assert.Equal(expected, actual);
    }
}