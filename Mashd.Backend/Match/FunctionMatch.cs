using Mashd.Backend.Value;

namespace Mashd.Backend.Match;

// TODO: Refactor this
public class FunctionMatch(TextValue identifier, params object[] arguments) : IMatch
{
    public TextValue Identifier { get; } = identifier;
    public object[] Arguments { get; } = arguments;
}