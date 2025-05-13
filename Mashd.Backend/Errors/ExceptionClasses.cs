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

