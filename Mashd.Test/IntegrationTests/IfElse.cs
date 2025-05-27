namespace Mashd.Test.IntegrationTests;

public class IfElse
{
    [Theory]
    [InlineData("1 < 2", 1L)]
    [InlineData("1 > 2", 0L)]
    public void IfWithoutElse(string condition, long expected)
    {
        // Arrange:
        string source = $@"
            Integer test = 0;
            if ({condition}) {{ test = 1; }}
        ";
        
        // Act
        var (interp, ast) = TestPipeline.Run(source);
        long actual = TestPipeline.GetInteger(interp, ast, "test");
        // Assert
        Assert.Equal(expected, actual);
    }
    
    [Theory]
    [InlineData("5 == 5", 1L)]
    [InlineData("5 != 5", 2L)]
    public void IfWithElse(string condition, long expected)
    {
        // Arrange:
        string source = $@"
            Integer test = 0;
            if ({condition}) {{ test = 1; }} else {{ test = 2; }}
        ";
    
        // Act
        var (interp, ast) = TestPipeline.Run(source);
        long actual = TestPipeline.GetInteger(interp, ast, "test");
        // Assert
        Assert.Equal(expected, actual);
    }
    
    [Theory]
    [InlineData(0, 1)]   // first branch
    [InlineData(1, 2)]   // second branch 
    [InlineData(2, 3)]   // third branch
    [InlineData(5, 5)]   // no branch → stays the same
    public void ElseIfChain_OnlyFirstMatchingBranchExecutes(int initial, int expected)
    {
        // Arrange:
        string source = $@"
            Integer test = {initial};
            if (test == 0)      {{ test = 1; }}
            else if (test == 1) {{ test = 2; }}
            else if (test == 2) {{ test = 3; }}
        ";

        // Act
        var (interpreter, ast) = TestPipeline.Run(source);
        long actual = TestPipeline.GetInteger(interpreter, ast, "test");

        // Assert
        Assert.Equal(expected, actual);
    }


}