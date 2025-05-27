namespace Mashd.Test.Fixtures;

public class IntegrationMashdFixture : IAsyncLifetime
{
    public string? TemporaryFilePath { get; set; } = null!;

    public Task InitializeAsync()
    {
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
    
    public string GenerateMashdFileWithValidCsvDataset(string filePath)
    {
        TemporaryFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".mashd");
        
        var csvContent = $$"""
                              Schema test = {
                                 id: {
                                     type: Integer,
                                     name: "ID"
                                 },
                                 firstName: {
                                     type: Text,
                                     name: "FirstName"
                                 },
                                 lastName: {
                                     type: Text,
                                     name: "LastName"
                                 }
                              };
                              
                              Dataset data = {
                                 schema: test,
                                 source: "{{ filePath }}",
                                 adapter: "csv",
                                 delimiter: ","
                              };
                           """;
        
        File.WriteAllText(TemporaryFilePath, csvContent);
        
        return TemporaryFilePath;
    }
    
    public string GenerateMashdFileWithInvalidCsvDataset(string filePath)
    {
        TemporaryFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".mashd");
        
        var csvContent = $$"""
                              Schema test = {
                                 id: {
                                     type: Integer,
                                     name: "ID"
                                 },
                                 firstName: {
                                     type: Integer,
                                     name: "FirstName"
                                 },
                                 lastName: {
                                     type: Integer,
                                     name: "LastName"
                                 }
                              };
                              
                              Dataset data = {
                                 schema: test,
                                 source: "{{ filePath }}",
                                 adapter: "csv",
                                 delimiter: ","
                              };
                           """;
        
        File.WriteAllText(TemporaryFilePath, csvContent);
        
        return TemporaryFilePath;
    }
    
    public string GenerateMashdFileWithValidPostgreSqlDataset(string filePath)
    {
        TemporaryFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".mashd");
        
        var csvContent = $$"""
                              Schema test = {
                                 id: {
                                     type: Integer,
                                     name: "ID"
                                 },
                                 firstName: {
                                     type: Text,
                                     name: "FirstName"
                                 },
                                 lastName: {
                                     type: Text,
                                     name: "LastName"
                                 }
                              };
                              
                              Dataset data = {
                                 schema: test,
                                 source: "{{ filePath }}",
                                 adapter: "postgresql",
                                 query: "SELECT * FROM Test"
                              };
                           """;
        
        File.WriteAllText(TemporaryFilePath, csvContent);
        
        return TemporaryFilePath;
    }
    
    public string GenerateMashdFileWithInvalidPostgreSqlDataset(string filePath)
    {
        TemporaryFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".mashd");
        
        var csvContent = $$"""
                              Schema test = {
                                 id: {
                                     type: Integer,
                                     name: "ID"
                                 },
                                 firstName: {
                                     type: Integer,
                                     name: "FirstName"
                                 },
                                 lastName: {
                                     type: Integer,
                                     name: "LastName"
                                 }
                              };
                              
                              Dataset data = {
                                 schema: test,
                                 source: "{{ filePath }}",
                                 adapter: "postgresql",
                                 query: "SELECT * FROM Test"
                              };
                           """;
        
        File.WriteAllText(TemporaryFilePath, csvContent);
        
        return TemporaryFilePath;
    }
}