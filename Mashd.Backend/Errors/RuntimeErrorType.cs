namespace Mashd.Backend.Errors;

public enum RuntimeErrorType
{
    Default,
    DivisionByZero,
    UndefinedVariable,
    TypeMismatch,
    ParseArity,
    ParseArgument,
    MethodNotFound,
    InvalidFilePath,
    DateFormat,
    UnsupportedOperation,
}