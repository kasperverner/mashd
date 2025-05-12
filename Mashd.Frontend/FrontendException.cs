namespace Mashd.Frontend;

public class FrontendException : Exception
{
    public IReadOnlyList<Error> Errors { get; }
    public ErrorType Phase { get; }
    public FrontendException(ErrorType phase, IEnumerable<Error> errors)
        : base($"Frontend errors in {phase}")
    {
        Phase = phase;
        Errors = errors.ToList().AsReadOnly();
    }
}
