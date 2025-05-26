namespace Mashd.Backend.Value;

public class PropertyAccessValue(SchemaFieldValue fieldValue, string identifier, string property) : IValue
{
    public SchemaFieldValue FieldValue { get; } = fieldValue;
    public string Identifier { get; } = identifier;
    public string Property { get; } = property;

    public override string ToString()
    {
        return $"{Identifier}.{Property}";
    }
}