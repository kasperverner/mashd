using Mashd.Backend.Value;

namespace Mashd.Backend.Errors;

public class FunctionReturnExceptionSignal : Exception
{
    public IValue ReturnValue { get; }

    public FunctionReturnExceptionSignal(IValue returnValue)
    {
        ReturnValue = returnValue;
    }
}