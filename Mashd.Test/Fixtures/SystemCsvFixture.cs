namespace Mashd.Test.Fixtures;

public class SystemCsvFixture : IAsyncLifetime
{
    public string SourceFilePath { get; private set; } = null!;
    public string DestinationFilePath { get; private set; } = null!;

    public Task InitializeAsync()
    {
        SourceFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".csv");
        DestinationFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".csv");
        
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
            
        File.WriteAllText(SourceFilePath, csvContent);
        
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        if (File.Exists(SourceFilePath))
        {
            File.Delete(SourceFilePath);
        }
        
        if (File.Exists(DestinationFilePath))
        {
            File.Delete(DestinationFilePath);
        }
        
        return Task.CompletedTask;
    }
}