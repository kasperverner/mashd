using Mashd.Frontend.AST;

namespace Mashd.Backend.Errors;

public class RuntimeException : Exception
{
    public AstNode Node { get; }
    public RuntimeErrorType ErrorType { get; }

    protected RuntimeException(AstNode node, RuntimeErrorType errorType = RuntimeErrorType.Default)
        : base(Format(node, errorType))
    {
        Node = node;
        ErrorType = errorType;
    }

    private static string Format(AstNode node, RuntimeErrorType errorType)
    {
        string location = $"line {node.Line}, column {node.Column}";
        string detail = errorType switch
        {
            RuntimeErrorType.DivisionByZero => "Division by zero",
            RuntimeErrorType.UndefinedVariable => "Use of undefined variable",
            RuntimeErrorType.TypeMismatch => "Type mismatch in operation",
            _ => errorType.ToString()
        };
        return $"Runtime error ({detail}) at {location}: `{node.Text}`";
    }
}