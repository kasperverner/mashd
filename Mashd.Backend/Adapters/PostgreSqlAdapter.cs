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
        if (!connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
            return connectionString;
        try
        {
            var uri = new Uri(connectionString);

            var userInfo = uri.UserInfo.Split(':');
            var username = userInfo.Length > 0 ? userInfo[0] : string.Empty;
            var password = userInfo.Length > 1 ? userInfo[1] : string.Empty;

            var connStrBuilder = new List<string>
            {
                $"Host={uri.Host}",
                $"Port={(uri.Port > 0 ? uri.Port : 5432)}",
                $"Database={uri.AbsolutePath.TrimStart('/')}"
            };
            
            if (!string.IsNullOrEmpty(username))
                connStrBuilder.Add($"Username={username}");
        
            if (!string.IsNullOrEmpty(password))
                connStrBuilder.Add($"Password={password}");
            
            var queryParams = System.Web.HttpUtility.ParseQueryString(uri.Query);
        
            foreach (string key in queryParams.AllKeys)
            {
                if (!string.IsNullOrEmpty(key))
                    connStrBuilder.Add($"{key}={queryParams[key]}");
            }
        
            return string.Join(";", connStrBuilder);
        }
        catch(Exception ex){
            throw new FormatException($"Invalid PostgreSQL URL format: {ex.Message}", ex);
        }
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