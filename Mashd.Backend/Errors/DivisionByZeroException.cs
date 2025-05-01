namespace Mashd.Backend.Errors;

public class DivisionByZeroException : RuntimeException
{
    public DivisionByZeroException(int line, int column, string sourceText = null)
        : base("Division by zero", line, column, sourceText)
    {
    }
    
}