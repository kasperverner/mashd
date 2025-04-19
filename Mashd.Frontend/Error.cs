namespace Mashd.Frontend;

public enum ErrorType
{
    Lexical,
    Syntactic,
    AstBuilder,
    NameResolution,
    TypeCheck,
    Interpretation
}

public class Error
{
    public ErrorType Type { get; }
    public int Line { get; }
    public int Column { get; }
    public string Message { get; }
    public string SourceText { get; }

    public Error(ErrorType type, int line, int column, string message, string sourceText)
    {
        Type = type;
        Line = line;
        Column = column;
        Message = message;
        SourceText = sourceText;
    }

    public override string ToString()
    {
        string header = string.Format("{0} Error {1}:{2} – {3}", Type, Line, Column, Message);
        if (!string.IsNullOrWhiteSpace(SourceText))
        {
            header = string.Concat(header, "  in `", SourceText.Trim(), "`");
        }

        return header;
    }
}