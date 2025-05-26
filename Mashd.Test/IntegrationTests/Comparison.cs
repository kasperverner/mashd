using Mashd.Frontend;

namespace Mashd.Test.IntegrationTests;

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
    
    [Fact]
    public void Ternary_Statement()
    {
        // Arrange
        string source = "Integer result = 1 > 0 ? 5 : 6;";
        
        // Act
        var (interp, ast) = TestPipeline.Run(source);
        
        // Assert
        long i = TestPipeline.GetInteger(interp, ast, "result");
        Assert.Equal(5L, i);
    }
    
    [Theory]
    // simple integer ternary
    [InlineData("Integer", "1 > 0 ? 5 : 6",     5L)]
    [InlineData("Integer", "0 > 1 ? 5 : 6",    6L)]
    // nested integer ternary
    [InlineData("Integer", "1 > 0 ? (0 > 1 ? 1 : 2) : 3", 2L)]
    [InlineData("Integer", "0 > 1 ? 1 : (1 > 0 ? 4 : 5)", 4L)]
    
    // decimal ternary
    [InlineData("Decimal", "1 > 0 ? 2.5 : 3.5",      2.5)]
    [InlineData("Decimal", "0 > 1 ? 2.5 : 3.5",     3.5)]
    
    // text ternary
    [InlineData("Text", "1 > 0 ? \"yes\" : \"no\"",  "yes")]
    [InlineData("Text", "0 > 1 ? \"yes\" : \"no\"", "no")]
    public void Ternary_ReturnsExpectedValue(string type, string expr, object expected)
    {
        // Arrange
        string source = $"{type} result = {expr};";
        
        // Act
        var (interp, ast) = TestPipeline.Run(source);
        
        // Assert
        switch (type)
        {
            case "Integer":
                Console.WriteLine("Integer case");
                long i = TestPipeline.GetInteger(interp, ast, "result");
                Assert.Equal((long)expected, i);
                break;
            case "Decimal":
                double d = TestPipeline.GetDecimal(interp, ast, "result");
                Assert.Equal((double)expected, d, precision: 10);
                break;
            case "Text":
                string s = TestPipeline.GetText(interp, ast, "result");
                Assert.Equal((string)expected, s);
                break;
        }
    }
    
    [Theory]
    [InlineData("Text t = 123;", ErrorType.TypeCheck)]
    [InlineData("Text t = 123.456;", ErrorType.TypeCheck)]
    [InlineData("Text t = -33;", ErrorType.TypeCheck)]

    public void Throws_TypeCheck_Error(string src, ErrorType expectedPhase)
    {
        // Arrange 
        var ex = Assert.Throws<FrontendException>(() =>
            TestPipeline.RunFull(src)
        );

        Assert.Equal(expectedPhase, ex.Phase);

        Assert.NotEmpty(ex.Errors);

        Assert.Contains("assign", ex.Errors[0].Message, StringComparison.OrdinalIgnoreCase);
    }

    
}