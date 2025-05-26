namespace Mashd.Backend.Value;

public class DatasetPlaceholderValue : IValue
{
    public string Name { get; }
    
    public DatasetPlaceholderValue(string name)
    {
        Name = name;
    }
    
    public override string ToString() => $"Dataset[{Name}]";
}