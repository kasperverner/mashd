using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Mashd.Backend.BuiltInMethods;
using System.Threading.Tasks;
using Mashd.Backend.Adapters;

namespace Mashd.Application
{
    public static class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Running CSV-based merge:");
            CsvBasedMerge();

            Console.WriteLine("\nRunning PostgreSQL-based merge:");
            await PostgresBasedMerge();
        }

        // ---------------------------------------------
        // EXAMPLE 1: MERGING CSV FILES
        // ---------------------------------------------
        static void CsvBasedMerge()
        {
            string root = Path.Combine(AppContext.BaseDirectory, @"..\..\..\..\Mashd.Backend\Data");
            string file1 = Path.GetFullPath(Path.Combine(root, "input1.csv"));
            string file2 = Path.GetFullPath(Path.Combine(root, "input2.csv"));
            string outputFile = Path.GetFullPath(Path.Combine(root, "merged_output_csv.csv"));

            var operations = LoadCsv(file1);
            var patients = LoadCsv(file2);

            operations = RemoveColumn(operations, "Notes");
            patients = RemoveColumn(patients, "Notes");

            operations = CastColumnToInt(operations, "Age");
            patients = CastColumnToInt(patients, "birth_year");

            operations = operations
                .Where(op => op.TryGetValue("Age", out var a) && int.TryParse(a, out int age) && age > 0 && age < 120)
                .ToList();

            patients = patients
                .Where(p => p.TryGetValue("birth_year", out var by) && int.TryParse(by, out int birthYear) && birthYear > 1900 && birthYear < 2025)
                .ToList();

            var merged = (
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

            WriteCsv(outputFile, merged);
            Console.WriteLine($"✅ CSV merged output written to: {outputFile}");
        }

        // ---------------------------------------------
        // EXAMPLE 2: MERGING FROM POSTGRESQL DATABASE
        // ---------------------------------------------
        static async Task PostgresBasedMerge()
        {
            string outputFile = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..\..\Mashd.Backend\Data\merged_output_postgres.csv"));

            string conn = "postgresql://postgres:Annedorte1@localhost:5432/patientdata";

            var patientAdapter = AdapterFactory.CreateAdapter("postgresql", new Dictionary<string, string>
            {
                { "connectionString", conn },
                { "query", "SELECT * FROM patients" }
            });

            var operationAdapter = AdapterFactory.CreateAdapter("postgresql", new Dictionary<string, string>
            {
                { "connectionString", conn },
                { "query", "SELECT * FROM operations" }
            });

            var patients = (await patientAdapter.ReadAsync())
                .Select(r => r.ToDictionary(k => k.Key, v => v.Value?.ToString() ?? "")).ToList();

            var operations = (await operationAdapter.ReadAsync())
                .Select(r => r.ToDictionary(k => k.Key, v => v.Value?.ToString() ?? "")).ToList();

            operations = CastColumnToInt(operations, "Age");
            patients = CastColumnToInt(patients, "birth_year");
            
            Console.WriteLine("Sample patients:");
            foreach (var p in patients.Take(3))
            {
                Console.WriteLine($"  {p["patient_id"]}: {p["fst_name"]} {p["lst_name"]}, birth_year={p["birth_year"]}");
            }

            Console.WriteLine("Sample operations:");
            foreach (var o in operations.Take(3))
            {
                Console.WriteLine($"  {o["Operation-ID"]}: {o["Patient-Name"]}, Age={o["Age"]}");
            }


            operations = operations
                .Where(op => op.TryGetValue("Age", out var a) && int.TryParse(a, out int age) && age > 0 && age < 120)
                .ToList();

            patients = patients
                .Where(p => p.TryGetValue("birth_year", out var by) && int.TryParse(by, out int birthYear) && birthYear > 1900 && birthYear < 2025)
                .ToList();

            var merged = (
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

            Console.WriteLine($"✅ Writing file to: {Path.GetFullPath(outputFile)}");
            Console.WriteLine($"Rows to write: {merged.Count}");

            WriteCsv(outputFile, merged);
            Console.WriteLine("✅ Done writing.");
            
        }

        // Load CSV as list of dictionaries
        static List<Dictionary<string, string>> LoadCsv(string path)
        {
            var result = new List<Dictionary<string, string>>();
            using var reader = new StreamReader(path);
            var headers = reader.ReadLine()?.Split(',').Select(h => h.Trim()).ToArray();

            if (headers == null)
                throw new InvalidOperationException("No headers found in CSV.");

            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                if (line == null) continue;

                var values = line.Split(',');
                var dict = new Dictionary<string, string>();
                for (int i = 0; i < headers.Length && i < values.Length; i++)
                {
                    dict[headers[i]] = values[i].Trim();
                }

                result.Add(dict);
            }

            return result;
        }

        // Remove a column from all rows
        static List<Dictionary<string, string>> RemoveColumn(List<Dictionary<string, string>> dataset, string column)
        {
            foreach (var row in dataset)
            {
                row.Remove(column);
            }

            return dataset;
        }

        // Cast a column to int, blanking out bad values
        static List<Dictionary<string, string>> CastColumnToInt(List<Dictionary<string, string>> dataset, string column)
        {
            foreach (var row in dataset)
            {
                if (row.TryGetValue(column, out var value))
                {
                    if (int.TryParse(value, out var parsed))
                        row[column] = parsed.ToString();
                    else
                        row[column] = "";
                }
            }

            return dataset;
        }

        // Write the result to CSV
        static void WriteCsv(string path, IEnumerable<Dictionary<string, string>> data)
        {
            if (!data.Any()) return;

            var headers = data.First().Keys.ToList();
            using var writer = new StreamWriter(path);
            writer.WriteLine(string.Join(",", headers));

            foreach (var row in data)
            {
                var line = string.Join(",", headers.Select(h => row.ContainsKey(h) ? row[h] : ""));
                writer.WriteLine(line);
            }
        }

        // Convert MM-dd-yyyy to dd-MM-yyyy
        static string ReformatDate(string mmddyyyy)
        {
            if (DateTime.TryParseExact(mmddyyyy, "MM-dd-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                return dt.ToString("dd-MM-yyyy");

            return mmddyyyy;
        }
    }
}
