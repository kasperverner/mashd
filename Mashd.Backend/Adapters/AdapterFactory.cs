using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mashd.Backend.Adapters;

public static class AdapterFactory
{
    public static IDataAdapter CreateAdapter(string adapterType, Dictionary<string, string> config)
    {
        return adapterType.ToLower() switch
        {
            "csv" => new CsvAdapter(
                filePath: config["source"], 
                delimiter: config.GetValueOrDefault("delimiter")
            ),
            "sqlserver" => new SqlServerAdapter(
                connectionString: config["source"],
                query: config["query"]
            ),
            "postgresql" => new PostgreSqlAdapter(
                connectionString: config["source"],
                query: config["query"]
            ),
            _ => throw new NotSupportedException($"Adapter type '{adapterType}' is not supported.")
        };
    }
}