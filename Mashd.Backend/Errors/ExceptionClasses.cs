using Mashd.Frontend.AST;

namespace Mashd.Backend.Errors;

public class DivisionByZeroException : RuntimeException
{
    public DivisionByZeroException(AstNode node)
        : base(node, RuntimeErrorType.DivisionByZero)
    {
    }
}

public class UndefinedVariableException : RuntimeException
{
    public UndefinedVariableException(AstNode node)
        : base(node, RuntimeErrorType.UndefinedVariable)
    {
    }
}

public class TypeMismatchException : RuntimeException
{
    public TypeMismatchException(AstNode node)
        : base(node, RuntimeErrorType.TypeMismatch) { }
}

public class ParseArityException : RuntimeException
{
    public int Expected { get; }
    public int Actual   { get; }

    public ParseArityException(AstNode node, int expected, int actual)
        : base(node, RuntimeErrorType.ParseArity)
    {
        Expected = expected;
        Actual   = actual;
    }
}

public class InvalidParseArgumentException : RuntimeException
{
    public SymbolType TargetType { get; }
    public Value      ArgValue   { get; }

    public InvalidParseArgumentException(AstNode node, SymbolType targetType, Value arg)
        : base(node, RuntimeErrorType.ParseArgument)
    {
        TargetType = targetType;
        ArgValue   = arg;
    }
}

public class MethodNotFoundException : RuntimeException
{
    public string CalledName   { get; }
    public Type   ReceiverType { get; }

    public MethodNotFoundException(AstNode node, string methodName, Type receiverType)
        : base(node, RuntimeErrorType.MethodNotFound)
    {
        CalledName   = methodName;
        ReceiverType = receiverType;
    }
}

public class InvalidFilePathException : RuntimeException
{
    public InvalidFilePathException(AstNode node)
        : base(node, RuntimeErrorType.InvalidFilePath)
    { }
}

public class DateFormatException : RuntimeException
{
    public string Text { get; }

    public DateFormatException(AstNode node, string text)
        : base(node, RuntimeErrorType.DateFormat)
    {
        Text = text;
    }
}

public class OperationNotSupportedException : RuntimeException
{
    public string Detail { get; }

    public OperationNotSupportedException(AstNode node, string detail)
        : base(node, RuntimeErrorType.UnsupportedOperation)
    {
        Detail = detail;
    }
}

