using Antlr4.Runtime;

namespace Mashd.Backend;

public class ExpressionParser
{
    public static MashdParser.ExpressionContext Parse(string input)
    {
        var inputStream = new AntlrInputStream(input);
        var lexer = new MashdLexer(inputStream);
        var tokenStream = new CommonTokenStream(lexer);
        var parser = new MashdParser(tokenStream);
        
        parser.RemoveErrorListeners();
        parser.AddErrorListener(new ThrowingErrorListener());
            
        return parser.expression();
    }
}
