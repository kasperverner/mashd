using Bogus;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Mashd.Test.Fixtures;

public class SystemPostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _sqlContainer = new PostgreSqlBuilder()
        .WithImage("postgres:latest")
        .WithDatabase("mashd")
        .WithUsername("postgres")
        .WithPassword("ch4ng3m3!")
        .Build();

    public string ConnectionString => _sqlContainer.GetConnectionString();
    
    public async Task InitializeAsync()
    {
        await _sqlContainer.StartAsync();
        await SeedDatabaseAsync();
    }

    public async Task DisposeAsync()
    {
        await _sqlContainer.DisposeAsync();
    }

    private async Task SeedDatabaseAsync()
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();

        await CreateTableAsync(connection);
        
        var existingCount = await GetPatientCountAsync(connection);
        if (existingCount == 0)
        {
            var patients = GeneratePatients();
            await InsertPatientsAsync(connection, patients);
        }
    }

    private static async Task CreateTableAsync(NpgsqlConnection connection)
    {
        const string createTableSql = """
            CREATE TABLE IF NOT EXISTS Patients (
                ID SERIAL PRIMARY KEY,
                FirstName TEXT NOT NULL,
                LastName TEXT NOT NULL,
                DateOfBirth DATE NOT NULL,
                Gender TEXT NOT NULL,
                SocialSecurityNumber TEXT NOT NULL,
                Email TEXT NULL,
                PhoneNumber TEXT NULL,
                EmergencyContactID INTEGER NULL,
                
                CONSTRAINT fk_emergency_contact 
                    FOREIGN KEY (EmergencyContactID) 
                    REFERENCES Patients(ID)
                    ON DELETE SET NULL
                    ON UPDATE CASCADE
            );
            """;

        await using var command = new NpgsqlCommand(createTableSql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<long> GetPatientCountAsync(NpgsqlConnection connection)
    {
        const string countSql = "SELECT COUNT(*) FROM Patients;";
        await using var command = new NpgsqlCommand(countSql, connection);
        return (long)(await command.ExecuteScalarAsync() ?? 0);
    }

    private static async Task InsertPatientsAsync(NpgsqlConnection connection, List<Patient> patients)
    {
        // First pass: Insert patients without emergency contact references
        await using var transaction = await connection.BeginTransactionAsync();
        
        try
        {
            const string insertSql = """
                INSERT INTO Patients (FirstName, LastName, DateOfBirth, Gender, SocialSecurityNumber, Email, PhoneNumber) 
                VALUES (@firstName, @lastName, @dateOfBirth, @gender, @socialSecurityNumber, @email, @phoneNumber)
                """;

            foreach (var patient in patients)
            {
                await using var command = new NpgsqlCommand(insertSql, connection, transaction);
                command.Parameters.AddWithValue("firstName", patient.FirstName);
                command.Parameters.AddWithValue("lastName", patient.LastName);
                command.Parameters.AddWithValue("dateOfBirth", patient.DateOfBirth.ToDateTime(TimeOnly.MinValue));
                command.Parameters.AddWithValue("gender", patient.Gender);
                command.Parameters.AddWithValue("socialSecurityNumber", patient.SocialSecurityNumber);
                command.Parameters.AddWithValue("email", (object?)patient.Email ?? DBNull.Value);
                command.Parameters.AddWithValue("phoneNumber", (object?)patient.PhoneNumber ?? DBNull.Value);
                
                await command.ExecuteNonQueryAsync();
            }

            // Second pass: Update some patients with emergency contact references
            var patientIds = await GetAllPatientIdsAsync(connection, transaction);
            await UpdateEmergencyContactsAsync(connection, transaction, patientIds);
            
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private static async Task<List<int>> GetAllPatientIdsAsync(NpgsqlConnection connection, NpgsqlTransaction transaction)
    {
        const string selectIdsSql = "SELECT ID FROM Patients ORDER BY ID;";
        await using var command = new NpgsqlCommand(selectIdsSql, connection, transaction);
        await using var reader = await command.ExecuteReaderAsync();
        
        var ids = new List<int>();
        while (await reader.ReadAsync())
        {
            ids.Add(reader.GetInt32(0));
        }
        return ids;
    }

    private static async Task UpdateEmergencyContactsAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, List<int> patientIds)
    {
        var random = new Random();
        const string updateSql = "UPDATE Patients SET EmergencyContactID = @emergencyContactId WHERE ID = @patientId;";

        foreach (var patientId in patientIds)
        {
            var emergencyContactId = patientIds.Where(id => id != patientId).MinBy(_ => random.Next());
            
            await using var command = new NpgsqlCommand(updateSql, connection, transaction);
            command.Parameters.AddWithValue("emergencyContactId", emergencyContactId);
            command.Parameters.AddWithValue("patientId", patientId);
            
            await command.ExecuteNonQueryAsync();
        }
    }

    private static List<Patient> GeneratePatients()
    {
        var faker = new Faker<Patient>()
            .RuleFor(p => p.FirstName, f => f.Name.FirstName())
            .RuleFor(p => p.LastName, f => f.Name.LastName())
            .RuleFor(p => p.DateOfBirth, f => DateOnly.FromDateTime(f.Date.Between(DateTime.Now.AddYears(-90), DateTime.Now.AddYears(-18))))
            .RuleFor(p => p.Gender, f => f.PickRandom("M", "F"))
            .RuleFor(p => p.SocialSecurityNumber, GenerateDanishCpr)
            .RuleFor(p => p.Email, f => f.Internet.Email())
            .RuleFor(p => p.PhoneNumber, f => f.Phone.PhoneNumber("+45 ########"))
            .RuleFor(p => p.EmergencyContactId, f => null);

        return faker.Generate(100);
    }

    private static string GenerateDanishCpr(Faker faker)
    {
        var birthDate = faker.Date.Between(DateTime.Now.AddYears(-90), DateTime.Now.AddYears(-18));
        var day = birthDate.Day.ToString("D2");
        var month = birthDate.Month.ToString("D2");
        var year = (birthDate.Year % 100).ToString("D2");
        var sequence = faker.Random.Number(1000, 9999).ToString();
        
        return $"{day}{month}{year}{sequence}";
    }
}

internal class Patient
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateOnly DateOfBirth { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string SocialSecurityNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public int? EmergencyContactId { get; set; }
}