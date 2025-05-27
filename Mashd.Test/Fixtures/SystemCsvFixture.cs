using System.Text;
using Bogus;

namespace Mashd.Test.Fixtures;

public class SystemCsvFixture : IAsyncLifetime
{
    public string SourceFilePath { get; private set; } = null!;
    public string OutputFilePath { get; private set; } = null!;

    public Task InitializeAsync()
    {
        SourceFilePath = CreateTempCsvFile();
        OutputFilePath = CreateTempCsvFile();
        
        var operations = GenerateOperations();
        WriteCsvFile(SourceFilePath, operations);
        
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        DeleteFileIfExists(SourceFilePath);
        DeleteFileIfExists(OutputFilePath);
        return Task.CompletedTask;
    }

    private static string CreateTempCsvFile()
    {
        return Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.csv");
    }

    private static void DeleteFileIfExists(string filePath)
    {
        if (File.Exists(filePath))
            File.Delete(filePath);
    }

    private static List<Operation> GenerateOperations()
    {
        var faker = new Faker<Operation>()
            .RuleFor(o => o.OperationId, f => f.IndexFaker + 1) // Sequential IDs starting from 1
            .RuleFor(o => o.PatientId, f => f.Random.Int(1, 100))
            .RuleFor(o => o.OperationType, f => f.PickRandom(GetDanishOperationTypes()))
            .RuleFor(o => o.OperationDate, f => f.Date.Between(DateTime.Now.AddYears(-2), DateTime.Now.AddMonths(3)))
            .RuleFor(o => o.SurgeonId, f => f.Random.Int(1, 25))
            .RuleFor(o => o.Duration, f => f.Random.Int(15, 480))
            .RuleFor(o => o.Status, (f, o) => GetRealisticStatus(f, o.OperationDate));

        return faker.Generate(200);
    }

    private static string[] GetDanishOperationTypes()
    {
        return
        [
            "Appendektomi",
            "Laparoskopisk kolecystektomi",
            "Herniereparation",
            "Knæartroskopi",
            "Sectio caesarea",
            "Hæmorroidektomi",
            "Tonsillektomi",
            "Katarakt operation",
            "Carpal tunnel release",
            "Hysterektomi",
            "Bypass operation",
            "Mastektomi",
            "Prostatektomi",
            "Diskektomi",
            "Rhinoplastik",
            "Cystoskopi",
            "Pacemaker implantation"
        ];
    }

    private static string GetRealisticStatus(Faker faker, DateTime operationDate)
    {
        var now = DateTime.Now;
        var daysDifference = (operationDate - now).Days;

        return daysDifference switch
        {
            // Future operations (more than 1 day in future)
            > 1 => faker.PickRandom("Planning", "Scheduled", "Scheduled", "Cancelled"),
            
            // Today or yesterday (could be in progress or just completed)
            >= -1 and <= 1 => faker.PickRandom("Scheduled", "In Progress", "Completed"),
            
            // Recent past (1-7 days ago) - mostly completed, some cancelled
            >= -7 and < -1 => faker.PickRandom("Completed", "Completed", "Completed", "Cancelled"),
            
            // Older past operations - completed or cancelled, rare postponed
            _ => faker.PickRandom("Completed", "Completed", "Completed", "Completed", "Cancelled", "Postponed")
        };
    }

    private static void WriteCsvFile(string filePath, List<Operation> operations)
    {
        var csv = new StringBuilder();
        csv.AppendLine("operation_id,patient_id,operation_type,operation_date,surgeon_id,duration,status");

        foreach (var operation in operations)
        {
            csv.AppendLine($"{operation.OperationId},{operation.PatientId},\"{operation.OperationType}\",{operation.OperationDate:yyyy-MM-dd},{operation.SurgeonId},{operation.Duration},\"{operation.Status}\"");
        }

        File.WriteAllText(filePath, csv.ToString(), Encoding.UTF8);
    }
}

internal class Operation
{
    public int OperationId { get; set; }
    public int PatientId { get; set; }
    public string OperationType { get; set; } = string.Empty;
    public DateTime OperationDate { get; set; }
    public int SurgeonId { get; set; }
    public int Duration { get; set; }
    public string Status { get; set; } = string.Empty;
}