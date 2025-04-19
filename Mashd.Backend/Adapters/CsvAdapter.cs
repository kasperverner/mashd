using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

namespace Mashd.Backend.Adapters;

public class CsvAdapter : IDataAdapter
{
    private readonly string _filePath;
    private readonly char _delimiter;

    public CsvAdapter(string filePath, string delimiter = ",")
    {
        _filePath = filePath;
        _delimiter = string.IsNullOrEmpty(delimiter) ? ',' : delimiter[0];
    }

    public async Task<IEnumerable<Dictionary<string, object>>> ReadAsync()
    {
        var result = new List<Dictionary<string, object>>();

        if (!File.Exists(_filePath))
            throw new FileNotFoundException($"CSV file not found at: {_filePath}");

        using var reader = new StreamReader(_filePath);

        string? headerLine = await reader.ReadLineAsync();
        if (headerLine == null)
            throw new InvalidOperationException("CSV file is empty.");

        var headers = headerLine.Split(_delimiter);

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            if (line == null) continue;

            var values = line.Split(_delimiter);
            var row = new Dictionary<string, object>();

            for (int i = 0; i < headers.Length && i < values.Length; i++)
            {
                row[headers[i].Trim()] = values[i].Trim();
            }

            result.Add(row);
        }

        return result;
    }
}