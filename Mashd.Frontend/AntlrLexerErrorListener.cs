using Antlr4.Runtime;

namespace Mashd.Frontend;

public class AntlrLexerErrorListener : IAntlrErrorListener<int>
{
    private readonly ErrorReporter _errors;

    public AntlrLexerErrorListener(ErrorReporter errors)
    {
        _errors = errors;
    }

    public void SyntaxError(
        TextWriter output,
        IRecognizer recognizer,
        int offendingSymbol,
        int line,
        int charPositionInLine,
        string msg,
        RecognitionException e)
    {
        string text = ((char)offendingSymbol).ToString();
        _errors.Report.Lexical(line, charPositionInLine, text, msg);
    }
}
