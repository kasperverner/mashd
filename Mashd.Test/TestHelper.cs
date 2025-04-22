using Antlr4.Runtime.Tree;

namespace TestProject1;

public static class TestHelper
{
    /**
     * This method retrieves the text of a terminal node from the parse tree.
     * It checks if the node is a TerminalNodeImpl and returns its text.
     * If not, it returns the text of the entire tree.
     */
    public static string GetTerminalText(IParseTree tree)
    {
        if (tree is TerminalNodeImpl terminal)
        {
            return terminal.Symbol.Text;
        }
            
        return tree.GetText();
    }
}