namespace Mashd.Test.Fixtures;

public class SystemMashdFixture : IAsyncLifetime
{
    private readonly List<string> _tempFiles = [];

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync()
    {
        foreach (var file in _tempFiles.Where(File.Exists))
            File.Delete(file);
        return Task.CompletedTask;
    }
    
    public string GenerateJoinMashdWithMatchConditions(string patientSource, string operationSource, string outputSource)
    {
        var mashdFilePath = CreateTempFile(".mashd");
        
        var mashdContent = $"""
            {PatientSchema}

            {OperationSchema}

            {CreatePatientDataset(patientSource)}

            {CreateOperationDataset(operationSource)}

            Mashd data = patients & operations;
            data.match(patients.id, operations.patient_id);
            
            Dataset output = data.join();
            
            output.toFile("{outputSource}");
            """;
        
        File.WriteAllText(mashdFilePath, mashdContent);
        return mashdFilePath;
    }
    
    public string GenerateJoinMashdWithTransform(string patientSource, string operationSource, string outputSource)
    {
        var mashdFilePath = CreateTempFile(".mashd");
        
        var mashdContent = $$"""
            {{PatientSchema}}

            {{OperationSchema}}

            {{CreatePatientDataset(patientSource)}}

            {{CreateOperationDataset(operationSource)}}
            
            Mashd data = patients & operations;
            data.transform({
                id: patients.id ?? operations.patient_id,
                firstName: patients.firstName,
                lastName: patients.lastName,
                operationType: operations.operation_type,
                operationDate: operations.operation_date,
                status: operations.status
            });

            Dataset output = data.join();
            
            output.toFile("{{outputSource}}");
            """;
        
        File.WriteAllText(mashdFilePath, mashdContent);
        return mashdFilePath;
    }
    
    public string GenerateJoinMashdWithMatchConditionsAndTransform(string patientSource, string operationSource, string outputSource)
    {
        var mashdFilePath = CreateTempFile(".mashd");
        
        var mashdContent = $$"""
            {{PatientSchema}}

            {{OperationSchema}}

            {{CreatePatientDataset(patientSource)}}

            {{CreateOperationDataset(operationSource)}}

            // Returns all operations for a surgeon with a given surgeonId
            Boolean findOperationsForSurgeon(Integer operationSurgeonId) {
                Integer surgeonId = 5;
                return surgeonId == operationSurgeonId;
            }
            
            Mashd data = patients & operations;
            data
                .match(patients.id, operations.patient_id)
                .functionMatch(findOperationsForSurgeon, operations.surgeon_id)
                .transform({
                    id: operations.surgeon_id,
                    firstName: patients.firstName,
                    lastName: patients.lastName,
                    operationType: operations.operation_type,
                    operationDate: operations.operation_date,
                    status: operations.status
                });

            Dataset output = data.join();
            
            output.toFile("{{outputSource}}");
            """;
        
        File.WriteAllText(mashdFilePath, mashdContent);
        return mashdFilePath;
    }
    
    public string GenerateUnionMashdWithMatchConditions(string patientSource, string outputSource)
    {
        var mashdFilePath = CreateTempFile(".mashd");
        
        var mashdContent = $"""
            {PatientSchema}

            {OperationSchema}

            {CreatePatientDataset(patientSource)}
            
            {CreateContactsDataset(patientSource)}

            Mashd data = patients & contacts;
            data.match(patients.emergencyContactId, contacts.id);
            
            Dataset output = data.union();
            
            output.toFile("{outputSource}");
            """;
        
        File.WriteAllText(mashdFilePath, mashdContent);
        return mashdFilePath;
    }
    
    public string GenerateUnionMashdWithTransform(string patientSource, string outputSource)
    {
        var mashdFilePath = CreateTempFile(".mashd");
        
        var mashdContent = $$"""
            {{PatientSchema}}

            {{OperationSchema}}

            {{CreatePatientDataset(patientSource)}}

            {{CreateContactsDataset(patientSource)}}
            
            Mashd data = patients & contacts;
            
            data.transform({
                id: patients.id ?? contacts.id,
                firstName: patients.firstName ?? contacts.firstName,
                lastName: patients.lastName ?? contacts.lastName
            });

            Dataset output = data.union();
            output.toFile("{{outputSource}}");
            """;
        
        File.WriteAllText(mashdFilePath, mashdContent);
        return mashdFilePath;
    }
    
    public string GenerateUnionMashdWithMatchConditionsAndTransform(string patientSource, string outputSource)
    {
        var mashdFilePath = CreateTempFile(".mashd");
        
        var mashdContent = $$"""
            {{PatientSchema}}

            {{OperationSchema}}

            {{CreatePatientDataset(patientSource)}}

            {{CreateContactsDataset(patientSource)}}
            
            Mashd data = patients & contacts;
            
            Text ContactName(Text firstName, Text lastName) {
                return firstName + " " + lastName;
            }
            
            data.match(patients.emergencyContactId, contacts.id);
            data.transform({
                id: patients.id ?? contacts.id,
                firstName: patients.firstName ?? contacts.firstName,
                lastName: patients.lastName ?? contacts.lastName,
                contactName: ContactName(contacts.firstName, contacts.lastName) ?? "Unknown"
            });
            
            Dataset output = data.union();
            output.toFile("{{outputSource}}");
            """;
        
        File.WriteAllText(mashdFilePath, mashdContent);
        return mashdFilePath;
    }

    private string CreateTempFile(string extension)
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}{extension}");
        _tempFiles.Add(filePath);
        return filePath;
    }

    private const string PatientSchema = """
        Schema patient = {
            id: { type: Integer, name: "ID" },
            firstName: { type: Text, name: "FirstName" },
            lastName: { type: Text, name: "LastName" },
            dateOfBirth: { type: Date, name: "DateOfBirth" },
            gender: { type: Text, name: "Gender" },
            socialSecurityNumber: { type: Text, name: "SocialSecurityNumber" },
            email: { type: Text, name: "Email" },
            phoneNumber: { type: Text, name: "PhoneNumber" },
            emergencyContactId: { type: Integer, name: "EmergencyContactID" }
        };
        """;

    private const string OperationSchema = """
        Schema operation = {
            operation_id: { type: Integer, name: "operation_id" },
            patient_id: { type: Integer, name: "patient_id" },
            operation_type: { type: Text, name: "operation_type" },
            operation_date: { type: Date, name: "operation_date" },
            surgeon_id: { type: Integer, name: "surgeon_id" },
            duration: { type: Integer, name: "duration" },
            status: { type: Text, name: "status" }
        };
        """;

    // language=text
    private static string CreatePatientDataset(string source) => $$"""
        Dataset patients = {
            schema: patient,
            source: "{{source}}",
            adapter: "postgresql",
            query: "SELECT * FROM Patients;"
        };
        """;
    
    // language=text
    private static string CreateContactsDataset(string source) => $$"""
        Dataset contacts = {
            schema: patient,
            source: "{{source}}",
            adapter: "postgresql",
            query: "SELECT * FROM Patients"
        };
        """;

    // language=text
    private static string CreateOperationDataset(string source) => $$"""
        Dataset operations = {
            schema: operation,
            source: "{{source}}",
            adapter: "csv",
            delimiter: ","
        };
        """;
}