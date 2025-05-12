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
        string input = "40 + 2";
            
        // Act
        var result = ExpressionParser.Parse(input);
            
        // Assert
        Assert.NotNull(result);
        Assert.Equal(input, TestHelper.GetTerminalText(result));
    }

[Fact]
    public void CanParseMultiplicationAndDivision()
    {
        // Arrange
        string[] inputs = new[]
        {
            "5 * 3",
            "10 / 2",
            "7 % 3"
        };
        
        // Act & Assert
        foreach (var input in inputs)
        {
            var result = ExpressionParser.Parse(input);
            Assert.NotNull(result);
        }
    }
    
    [Fact]
    public void CanParseNestedArithmeticExpressions()
    {
        // Arrange
        string input = "2 + 3 * 4 - 5 / 2";
        
        // Act
        var result = ExpressionParser.Parse(input);
        
        // Assert
        Assert.NotNull(result);
    }
    
    [Fact]
    public void CanParseParenthesizedExpressions()
    {
        // Arrange
        string input = "(2 + 3) * (4 - 1)";
        
        // Act
        var result = ExpressionParser.Parse(input);
        
        // Assert
        Assert.NotNull(result);
    }
    
    [Fact]
    public void CanParseLiterals()
    {
        // Arrange
        string[] inputs = new[]
        {
            "42",              // integer
            "3.14",            // decimal
            "\"Hello, world\"", // text
            "true",            // boolean
            "false",           // boolean
            "null"             // null
        };
        
        // Act & Assert
        foreach (var input in inputs)
        {
            var result = ExpressionParser.Parse(input);
            Assert.NotNull(result);
        }
    }
    
    [Fact]
    public void CanParseDateLiteral()
    {
        // Arrange
        string input = "\"2023-05-12T14:30:00Z\"";
        
        // Act
        var result = ExpressionParser.Parse(input);
        
        // Assert
        Assert.NotNull(result);
    }
    
    [Fact]
    public void CanParseComparisonExpressions()
    {
        // Arrange
        string[] inputs = new[]
        {
            "x < 10",
            "y <= 20",
            "z > 0",
            "value >= 100",
            "a == b",
            "c != d"
        };
        
        // Act & Assert
        foreach (var input in inputs)
        {
            var result = ExpressionParser.Parse(input);
            Assert.NotNull(result);
        }
    }
    
    [Fact]
    public void CanParseLogicalExpressions()
    {
        // Arrange
        string[] inputs = new[]
        {
            "a && b",
            "c || d",
            "x > 0 && y < 10",
            "isValid || hasPermission"
        };
        
        // Act & Assert
        foreach (var input in inputs)
        {
            var result = ExpressionParser.Parse(input);
            Assert.NotNull(result);
        }
    }
    
    [Fact]
    public void CanParseUnaryExpressions()
    {
        // Arrange
        string[] inputs = new[]
        {
            "-x",
            "!isValid"
        };
        
        // Act & Assert
        foreach (var input in inputs)
        {
            var result = ExpressionParser.Parse(input);
            Assert.NotNull(result);
        }
    }
    
    [Fact]
    public void CanParseTernaryExpression()
    {
        // Arrange
        string input = "isValid ? \"Success\" : \"Error\"";
        
        // Act
        var result = ExpressionParser.Parse(input);
        
        // Assert
        Assert.NotNull(result);
    }
    
    [Fact]
    public void CanParseNullishCoalescingExpression()
    {
        // Arrange
        string input = "value ?? defaultValue";
        
        // Act
        var result = ExpressionParser.Parse(input);
        
        // Assert
        Assert.NotNull(result);
    }
    
    [Fact]
    public void CanParseFunctionCall()
    {
        // Arrange
        string input = "calculateTotal(price, quantity)";
        
        // Act
        var result = ExpressionParser.Parse(input);
        
        // Assert
        Assert.NotNull(result);
    }
    
    [Fact]
    public void CanParseFunctionCallWithNoArguments()
    {
        // Arrange
        string input = "getCurrentDate()";
        
        // Act
        var result = ExpressionParser.Parse(input);
        
        // Assert
        Assert.NotNull(result);
    }
    
    [Fact]
    public void CanParsePropertyAccess()
    {
        // Arrange
        string input = "person.name";
        
        // Act
        var result = ExpressionParser.Parse(input);
        
        // Assert
        Assert.NotNull(result);
    }
    
    [Fact]
    public void CanParseMethodCall()
    {
        // Arrange
        string input = "person.getName()";
        
        // Act
        var result = ExpressionParser.Parse(input);
        
        // Assert
        Assert.NotNull(result);
    }
    
    [Fact]
    public void CanParseChainedMethodCalls()
    {
        // Arrange
        string input = "data.filter(x > 10).sort()";
        
        // Act
        var result = ExpressionParser.Parse(input);
        
        // Assert
        Assert.NotNull(result);
    }
    
    [Fact]
    public void CanParseObjectExpression()
    {
        // Arrange
        string input = "{ name: \"John\", age: 30, isActive: true }";
        
        // Act
        var result = ExpressionParser.Parse(input);
        
        // Assert
        Assert.NotNull(result);
    }
    
    [Fact]
    public void CanParseEmptyObjectExpression()
    {
        // Arrange
        string input = "{}";
        
        // Act
        var result = ExpressionParser.Parse(input);
        
        // Assert
        Assert.NotNull(result);
    }
    
    [Fact]
    public void CanParseDatasetCombineExpression()
    {
        // Arrange
        string input = "usersDataset & ordersDataset";
        
        // Act
        var result = ExpressionParser.Parse(input);
        
        // Assert
        Assert.NotNull(result);
    }
    
    [Fact]
    public void CanParseComplexExpression()
    {
        // Arrange
        string input = "(x > 0 && y < 10) ? calculate(x + y) * 2 : defaultValue ?? 0";
        
        // Act
        var result = ExpressionParser.Parse(input);
        
        // Assert
        Assert.NotNull(result);
    }
    
    [Fact]
    public void CanParseTypeLiteral()
    {
        // Arrange
        string[] inputs = new[]
        {
            "Integer",
            "Boolean",
            "Text",
            "Date",
            "Decimal"
        };
        
        // Act & Assert
        foreach (var input in inputs)
        {
            var result = ExpressionParser.Parse(input);
            Assert.NotNull(result);
        }
    }
    
    [Fact]
    public void CanParseNestedObjectExpressions()
    {
        // Arrange
        string input = "{ user: { name: \"John\", address: { city: \"Copenhagen\", zip: \"10001\" } } }";
        
        // Act
        var result = ExpressionParser.Parse(input);
        
        // Assert
        Assert.NotNull(result);
    }
    
    [Fact]
    public void CanParseFunctionCallsWithComplexArguments()
    {
        // Arrange
        string input = "processData(x + y, { id: userId, options: { sort: true } })";
        
        // Act
        var result = ExpressionParser.Parse(input);
        
        // Assert
        Assert.NotNull(result);
    }
}