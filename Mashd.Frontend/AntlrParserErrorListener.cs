using Antlr4.Runtime;

namespace Mashd.Frontend;

public class AntlrParserErrorListener : BaseErrorListener
{
    private readonly ErrorReporter _errors;

    public AntlrParserErrorListener(ErrorReporter errors)
    {
        _errors = errors;
    }

    public override void SyntaxError(
        TextWriter output,
        IRecognizer recognizer,
        IToken offendingSymbol,
        int line,
        int charPositionInLine,
        string msg,
        RecognitionException e)
    {
        string text = offendingSymbol.Text;
        _errors.Report.Syntactic(line, charPositionInLine, text, msg);
    }
}
