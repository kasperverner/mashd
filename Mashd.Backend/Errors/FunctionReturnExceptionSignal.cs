namespace Mashd.Backend.Errors;

public class FunctionReturnExceptionSignal : Exception
{
    public Value ReturnValue { get; }

    public FunctionReturnExceptionSignal(Value returnValue)
    {
        ReturnValue = returnValue;
    }
}