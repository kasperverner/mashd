namespace Mashd.Test.Fixtures;

public class SystemMashdFixture : IAsyncLifetime
{
    private readonly List<string> _sourceFilePaths = [];

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        foreach (var sourceFilePath in _sourceFilePaths.Where(File.Exists))
            File.Delete(sourceFilePath);

        return Task.CompletedTask;
    }
    
    public string GenerateJoinMashdWithMatchConditions(string patientFilePath, string operationFilePath, string outputFilePath)
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".mashd");
        
        var csvContent = 
            $$"""
            {{PatientSchema}}

            {{OperationSchema}}

            {{PatientDataset(patientFilePath)}}

            {{OperationDataset(operationFilePath)}}

            Mashd data = patients & operations;

            data.match(patients.id, operations.patientId);
            
            Dataset output = data.join();
            
            output.toFile("{{outputFilePath}}");
            """;
        
        File.WriteAllText(path, csvContent);
        
        _sourceFilePaths.Add(path);
        
        return path;
    }
    
    private const string PatientSchema = 
        """
        Schema patient = {
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
            },
            age: {
                type: Integer,
                name: "Age"
            }
        };
        """;

    private static readonly Func<string, string> PatientDataset = (string source) => $$"""
                                                                                     Dataset patients = {
                                                                                         schema: patient,
                                                                                         source: "{{ source }}",
                                                                                         adapter: "csv",
                                                                                         delimiter: ","
                                                                                     };
                                                                                     """;

    private const string OperationSchema = """
                                           Schema operation = {
                                               id: {
                                                   type: Integer,
                                                   name: "ID"
                                               },
                                               patientId: {
                                                   type: Integer,
                                                   name: "PatientID"
                                               },
                                               operationDate: {
                                                   type: DateTime,
                                                   name: "OperationDate"
                                               },
                                               description: {
                                                   type: Text,
                                                   name: "Description"
                                               }
                                           };
                                           """;
    
    private static readonly Func<string, string> OperationDataset = (string source) => $$"""
                                                                                         Dataset operations = {
                                                                                             schema: operation,
                                                                                             source: "{{ source }}",
                                                                                             adapter: "csv",
                                                                                             delimiter: ","
                                                                                         };
                                                                                         """;
}