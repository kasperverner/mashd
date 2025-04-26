using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mashd.Backend.Adapters;

public class PostgreSqlAdapter : IDataAdapter
{
    private readonly string _connectionString;
    private readonly string _query;

    public PostgreSqlAdapter(string connectionString, string query)
    {
        _connectionString = NormalizeConnectionString(connectionString);
        _query = query;
    }

    private static string NormalizeConnectionString(string connectionString)
    {
        if (connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            var uri = new Uri(connectionString);

            var userInfo = uri.UserInfo.Split(':');
            var username = userInfo[0];
            var password = userInfo.Length > 1 ? userInfo[1] : "";

            var host = uri.Host;
            var port = uri.Port;
            var database = uri.AbsolutePath.TrimStart('/');

            return $"Host={host};Port={port};Username={username};Password={password};Database={database}";
        }
        
        return connectionString;
    }

    public async Task<IEnumerable<Dictionary<string, object>>> ReadAsync()
    {
        var results = new List<Dictionary<string, object>>();

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(_query, connection);
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