using Antlr4.Runtime.Tree;
using Mashd.Backend;

namespace TestProject1;

public class UnitTests
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
        Assert.Equal("42", GetTerminalText(result));
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
        Assert.Equal("40 + 2", GetTerminalText(result));
    }
    
    /** 
     * This method retrieves the text of a terminal node from the parse tree.
     * It checks if the node is a TerminalNodeImpl and returns its text.
     * If not, it returns the text of the entire tree.
     */
    private string GetTerminalText(IParseTree tree)
    {
        if (tree is TerminalNodeImpl terminal)
        {
            return terminal.Symbol.Text;
        }
            
        return tree.GetText();
    }
}