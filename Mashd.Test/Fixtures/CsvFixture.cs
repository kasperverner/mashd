namespace Mashd.Test.Fixtures;

public class CsvFixture : IAsyncLifetime
{
    public string TemporaryFilePath { get; set; } = null!;

    public Task InitializeAsync()
    {
        TemporaryFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".csv");
        
        const string csvContent = """
                                     ID,FirstName,LastName
                                     1,John,Doe
                                     2,Jane,Smith
                                     3,Alice,Johnson
                                     4,Bob,Brown
                                     5,Charlie,Davis
                                     6,Eve,Wilson
                                     7,Frank,Garcia
                                     8,Grace,Martinez
                                     9,Heidi,Lopez
                                     10,Ivan,Gonzalez
                                  """;
            
        File.WriteAllText(TemporaryFilePath, csvContent);
        
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        if (File.Exists(TemporaryFilePath))
        {
            File.Delete(TemporaryFilePath);
        }
        
        return Task.CompletedTask;
    }
}