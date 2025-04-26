using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mashd.Backend.Adapters;

public class SqlServerAdapter : IDataAdapter
{
    private readonly string _connectionString;
    private readonly string _query;

    public SqlServerAdapter(string connectionString, string query)
    {
        _connectionString = connectionString;
        _query = query;
    }

    public async Task<IEnumerable<Dictionary<string, object>>> ReadAsync()
    {
        var results = new List<Dictionary<string, object>>();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand(_query, connection);
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var row = new Dictionary<string, object>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                row[reader.GetName(i)] = reader.GetValue(i);
            }

            results.Add(row);
        }

        return results;
    }
}