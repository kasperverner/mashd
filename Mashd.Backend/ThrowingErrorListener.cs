using Antlr4.Runtime;

namespace Mashd.Backend;

public class ThrowingErrorListener : BaseErrorListener
{
    public override void SyntaxError(
        TextWriter output,
        IRecognizer recognizer,
        IToken offendingSymbol,
        int line,
        int charPositionInLine,
        string msg,
        RecognitionException e)
    {
        throw new ParseException(msg, line, charPositionInLine);
    }
}