namespace Mashd.Backend.Adapters;

public interface IDataAdapter
{
    Task<IEnumerable<Dictionary<string, object>>> ReadAsync();
}