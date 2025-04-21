namespace Mashd.Backend;

public class ParseException(String message, int line, int column) : Exception(message)
{
    public int Line { get; } = line;
    public int Column { get; } = column;
}