namespace Mashd.Backend;

public record RowContext(string LeftIdentifier, Dictionary<string, object> LeftRow, string RightIdentifier, Dictionary<string, object> RightRow);
