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
            "sqlserver" => new SqlServerAdapter(
                connectionString: config["connectionString"],
                query: config["query"]
            ),
            "csv" => new CsvAdapter(
                config["file"], config.ContainsKey("delimiter") ? config["delimiter"] : ","
            ),
            "postgresql" => new PostgreSqlAdapter(
                config["connectionString"],
                config["query"]
            ),
            _ => throw new NotSupportedException($"Adapter type '{adapterType}' is not supported.")
        };
    }
}