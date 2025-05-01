using Mashd.Frontend.AST;

namespace Mashd.Backend.Errors;

public class RuntimeException : Exception
{
    public int Line { get; }
    public int Column { get; }
    public string SourceText { get; }
    public ExceptionType Type { get; } = ExceptionType.Runtime;
    public RuntimeException(string message, int line, int column, string sourceText = null)
        : base(message)
    {
        Line = line;
        Column = column;
        SourceText = sourceText;
    }
    
}

