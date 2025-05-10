using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Mashd.Backend.Adapters;
using Mashd.Backend.BuiltInMethods;
using Npgsql;

namespace Mashd.Application
{
    public static class Program
    {
        static async Task Main(string[] args)
        {
            string outputFormat = "postgres"; // Options are "csv", "postgres" and "sql"

            switch (outputFormat.ToLower())
            {
                case "csv":
                    Console.WriteLine("Running CSV-based merge:");
                    await RunMergeAsync("csv", outputFormat);
                    break;

                case "postgres":
                    Console.WriteLine("Running PostgreSQL-based merge:");
                    await RunMergeAsync("postgresql", outputFormat);
                    break;

                case "sql":
                    Console.WriteLine("Running SQL Server-based merge:");
                    await RunMergeAsync("sqlserver", outputFormat);
                    break;

                default:
                    Console.WriteLine("Invalid output format specified.");
                    break;
            }
        }

        static async Task RunMergeAsync(string sourceType, string outputFormat)
        {
            string outputPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Mashd.Backend", "Data", $"merged_output_{outputFormat.ToLower()}.csv");

            List<Dictionary<string, string>> patients;
            List<Dictionary<string, string>> operations;

            if (sourceType == "csv")
            {
                string root = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Mashd.Backend", "Data");
                string file1 = Path.GetFullPath(Path.Combine(root, "input1.csv"));
                string file2 = Path.GetFullPath(Path.Combine(root, "input2.csv"));
                operations = LoadCsv(file1);
                patients = LoadCsv(file2);
            }
            else
            {
                string conn = sourceType == "postgresql"
                    ? "postgresql://postgres:Annedorte1@localhost:5432/patientdata"
                    : "Server=localhost;Database=patientdata;User Id=sa;Password=Annedorte1;TrustServerCertificate=True";

                var patientAdapter = AdapterFactory.CreateAdapter(sourceType, new Dictionary<string, string>
                {
                    { "connectionString", conn },
                    { "query", "SELECT * FROM patients" }
                });

                var operationAdapter = AdapterFactory.CreateAdapter(sourceType, new Dictionary<string, string>
                {
                    { "connectionString", conn },
                    { "query", "SELECT * FROM operations" }
                });

                patients = (await patientAdapter.ReadAsync()).Select(r => r.ToDictionary(k => k.Key, v => v.Value?.ToString() ?? "")).ToList();
                operations = (await operationAdapter.ReadAsync()).Select(r => r.ToDictionary(k => k.Key, v => v.Value?.ToString() ?? "")).ToList();
            }

            operations = RemoveColumn(operations, "Notes");
            patients = RemoveColumn(patients, "Notes");

            operations = CastColumnToInt(operations, "Age");
            patients = CastColumnToInt(patients, "birth_year");

            operations = operations.Where(op => op.TryGetValue("Age", out var a) && int.TryParse(a, out int age) && age > 0 && age < 120).ToList();
            patients = patients.Where(p => p.TryGetValue("birth_year", out var by) && int.TryParse(by, out int birthYear) && birthYear > 1900 && birthYear < 2025).ToList();

            var merged = MergePatientsAndOperations(patients, operations);

            if (outputFormat.ToLower() == "postgres")
            {
                await WriteToPostgres(merged);
            }
            else
            {
                WriteCsv(outputPath, merged);
            }
        }

        static async Task WriteToPostgres(List<Dictionary<string, string>> data)
        {
            string connStr = "Host=localhost;Port=5432;Database=patientdata;Username=postgres;Password=Annedorte1";
            await using var conn = new NpgsqlConnection(connStr);
            await conn.OpenAsync();

            string drop = "DROP TABLE IF EXISTS merged_output;";
            string create = @"
                CREATE TABLE merged_output (
                    id TEXT,
                    name TEXT,
                    operationId TEXT,
                    operationType TEXT,
                    operationDate TEXT
                );";

            await using (var cmd = new NpgsqlCommand(drop + create, conn))
                await cmd.ExecuteNonQueryAsync();

            foreach (var row in data)
            {
                var insert = "INSERT INTO merged_output (id, name, operationId, operationType, operationDate) VALUES (@id, @name, @operationId, @operationType, @operationDate);";
                await using var insertCmd = new NpgsqlCommand(insert, conn);
                insertCmd.Parameters.AddWithValue("@id", row["id"]);
                insertCmd.Parameters.AddWithValue("@name", row["name"]);
                insertCmd.Parameters.AddWithValue("@operationId", row["operationId"]);
                insertCmd.Parameters.AddWithValue("@operationType", row["operationType"]);
                insertCmd.Parameters.AddWithValue("@operationDate", row["operationDate"]);
                await insertCmd.ExecuteNonQueryAsync();
            }
        }

        static List<Dictionary<string, string>> MergePatientsAndOperations(List<Dictionary<string, string>> patients, List<Dictionary<string, string>> operations)
        {
            return (
                from patient in patients
                let fullName = $"{patient["fst_name"]} {patient["lst_name"]}"
                let birthYear = int.Parse(patient["birth_year"])
                from op in operations
                let opName = op["Patient-Name"]
                let opAge = int.Parse(op["Age"])
                let estBirth = 2023 - opAge
                where FuzzyMatchMethod.FuzzyMatch(fullName, opName, 0.2)
                      && Math.Abs(birthYear - estBirth) <= 1
                select new Dictionary<string, string>
                {
                    ["id"] = patient["patient_id"],
                    ["name"] = fullName,
                    ["operationId"] = op["Operation-ID"],
                    ["operationType"] = op["Operation-Name"],
                    ["operationDate"] = ReformatDate(op["Date"])
                }
            ).ToList();
        }

        static List<Dictionary<string, string>> LoadCsv(string path)
        {
            var result = new List<Dictionary<string, string>>();
            using var reader = new StreamReader(path);
            var headers = reader.ReadLine()?.Split(',').Select(h => h.Trim()).ToArray();
            if (headers == null) throw new InvalidOperationException("No headers found in CSV.");

            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                if (line == null) continue;
                var values = line.Split(',');
                var dict = new Dictionary<string, string>();
                for (int i = 0; i < headers.Length && i < values.Length; i++)
                    dict[headers[i]] = values[i].Trim();
                result.Add(dict);
            }
            return result;
        }

        static List<Dictionary<string, string>> RemoveColumn(List<Dictionary<string, string>> dataset, string column)
        {
            foreach (var row in dataset) row.Remove(column);
            return dataset;
        }

        static List<Dictionary<string, string>> CastColumnToInt(List<Dictionary<string, string>> dataset, string column)
        {
            foreach (var row in dataset)
            {
                if (row.TryGetValue(column, out var value))
                    row[column] = int.TryParse(value, out var parsed) ? parsed.ToString() : "";
            }
            return dataset;
        }

        static void WriteCsv(string path, IEnumerable<Dictionary<string, string>> data)
        {
            var dataList = data.ToList();
            if (!dataList.Any()) return;

            var headers = dataList.First().Keys.ToList();
            using var writer = new StreamWriter(path);
            writer.WriteLine(string.Join(",", headers));
            foreach (var row in dataList)
                writer.WriteLine(string.Join(",", headers.Select(h => row.ContainsKey(h) ? row[h] : "")));
        }

        static string ReformatDate(string mmddyyyy)
        {
            return DateTime.TryParseExact(mmddyyyy, "MM-dd-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt)
                ? dt.ToString("dd-MM-yyyy")
                : mmddyyyy;
        }
    }
}
