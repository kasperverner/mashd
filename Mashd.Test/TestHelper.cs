using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Mashd.Backend;

namespace Mashd.Test;

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
    
    public static MashdParser CreateParser(string input)
    {
        var parser = new MashdParser(new CommonTokenStream(new MashdLexer(new AntlrInputStream(input))));
        parser.RemoveErrorListeners();
        parser.AddErrorListener(new ThrowingErrorListener());
        parser.ErrorHandler = new BailErrorStrategy();
        return parser; 
    }
}