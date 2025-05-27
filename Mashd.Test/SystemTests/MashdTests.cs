using System.Diagnostics;
using Mashd.Backend.Adapters;
using Mashd.Test.Fixtures;
using Mashd.Test.IntegrationTests;
using Xunit.Abstractions;

namespace Mashd.Test.SystemTests;

public class MashdTests(ITestOutputHelper testOutputHelper, SystemCsvFixture csv, SystemPostgreSqlFixture db, SystemMashdFixture mashd) 
    : IClassFixture<SystemCsvFixture>, IClassFixture<SystemPostgreSqlFixture>, IClassFixture<SystemMashdFixture>
{
    [Fact]
    public async Task Join_Datasets_With_Match_Conditions()
    {
        long start = Stopwatch.GetTimestamp();
        
        // Arrange
        var patientSource = db.ConnectionString;
        var operationSource = csv.SourceFilePath;
        var outputSource = csv.OutputFilePath;
        var mashdSource = mashd.GenerateJoinMashdWithMatchConditions(patientSource, operationSource, outputSource);
        
        // Act
        var content = await File.ReadAllTextAsync(mashdSource);

        TestPipeline.Run(content);
        
        var data = (await AdapterFactory.CreateAdapter("csv", new Dictionary<string, string>
        {
            { "source", outputSource },
            { "delimiter", "," }
        }).ReadAsync()).ToArray();

        var firstRow = data.First();
        
        // Assert
        Assert.NotEmpty(data);
        Assert.Equal(16, firstRow.Count);

        var ticks = Stopwatch.GetTimestamp() - start;
        var elapsedTime = ticks/10000;
        testOutputHelper.WriteLine($"Elapsed time for 'Join_Datasets_With_Match_Conditions': {elapsedTime}ms");
    }

    [Fact]
    public async Task Join_Datasets_With_Transform()
    {
        long start = Stopwatch.GetTimestamp();
        
        // Arrange
        var patientSource = db.ConnectionString;
        var operationSource = csv.SourceFilePath;
        var outputSource = csv.OutputFilePath;
        var mashdSource = mashd.GenerateJoinMashdWithTransform(patientSource, operationSource, outputSource);
        
        // Act
        var content = await File.ReadAllTextAsync(mashdSource);
        TestPipeline.Run(content);
        
        var data = (await AdapterFactory.CreateAdapter("csv", new Dictionary<string, string>
        {
            { "source", outputSource },
            { "delimiter", "," }
        }).ReadAsync()).ToArray();
        
        var firstRow = data.First();
        
        // Assert
        Assert.NotEmpty(data);
        Assert.Equal(6, firstRow.Count);
        
        var ticks = Stopwatch.GetTimestamp() - start;
        var elapsedTime = ticks/10000;
        testOutputHelper.WriteLine($"Elapsed time for 'Join_Datasets_With_Transform': {elapsedTime}ms");
    }

    [Fact]
    public async Task Join_Datasets_With_Match_Conditions_And_Transform()
    {
        long start = Stopwatch.GetTimestamp();
        
        // Arrange
        var patientSource = db.ConnectionString;
        var operationSource = csv.SourceFilePath;
        var outputSource = csv.OutputFilePath;
        var mashdSource = mashd.GenerateJoinMashdWithMatchConditionsAndTransform(patientSource, operationSource, outputSource);
        
        // Act
        var content = await File.ReadAllTextAsync(mashdSource);
        TestPipeline.Run(content);
        
        var data = (await AdapterFactory.CreateAdapter("csv", new Dictionary<string, string>
        {
            { "source", outputSource },
            { "delimiter", "," }
        }).ReadAsync()).ToArray();
        
        var firstRow = data.First();
        
        // Assert
        Assert.NotEmpty(data);
        Assert.Equal(6, firstRow.Count);
        
        var ticks = Stopwatch.GetTimestamp() - start;
        var elapsedTime = ticks/10000;
        testOutputHelper.WriteLine($"Elapsed time for 'Join_Datasets_With_Match_Conditions_And_Transform': {elapsedTime}ms");
    }
    
    [Fact]
    public async Task Union_Datasets_With_Match_Conditions()
    {
        long start = Stopwatch.GetTimestamp();
        
        // Arrange
        var patientSource = db.ConnectionString;
        var outputSource = csv.OutputFilePath;
        var mashdSource = mashd.GenerateUnionMashdWithMatchConditions(patientSource, outputSource);
        
        // Act
        var content = await File.ReadAllTextAsync(mashdSource);
        TestPipeline.Run(content);
        
        var data = (await AdapterFactory.CreateAdapter("csv", new Dictionary<string, string>
        {
            { "source", outputSource },
            { "delimiter", "," }
        }).ReadAsync()).ToArray();
        
        var firstRow = data.First();
        
        // Assert
        Assert.NotEmpty(data);
        Assert.Equal(9, firstRow.Count);
        
        var ticks = Stopwatch.GetTimestamp() - start;
        var elapsedTime = ticks/10000;
        testOutputHelper.WriteLine($"Elapsed time for 'Union_Datasets_With_Match_Conditions': {elapsedTime}ms");
    }

    [Fact]
    public async Task Union_Datasets_With_Transform()
    {
        long start = Stopwatch.GetTimestamp();
        
        // Arrange
        var patientSource = db.ConnectionString;
        var outputSource = csv.OutputFilePath;
        var mashdSource = mashd.GenerateUnionMashdWithTransform(patientSource, outputSource);
        
        // Act
        var content = await File.ReadAllTextAsync(mashdSource);
        TestPipeline.Run(content);
        
        var data = (await AdapterFactory.CreateAdapter("csv", new Dictionary<string, string>
        {
            { "source", outputSource },
            { "delimiter", "," }
        }).ReadAsync()).ToArray();
        
        var firstRow = data.First();
        
        // Assert
        Assert.NotEmpty(data);
        Assert.Equal(3, firstRow.Count);
        
        var ticks = Stopwatch.GetTimestamp() - start;
        var elapsedTime = ticks/10000;
        testOutputHelper.WriteLine($"Elapsed time for 'Union_Datasets_With_Transform': {elapsedTime}ms");
    }

    [Fact]
    public async Task Union_Datasets_With_Match_Conditions_And_Transform()
    {
        long start = Stopwatch.GetTimestamp();
        
        // Arrange
        var patientSource = db.ConnectionString;
        var outputSource = csv.OutputFilePath;
        var mashdSource = mashd.GenerateUnionMashdWithMatchConditionsAndTransform(patientSource, outputSource);
        
        // Act
        var content = await File.ReadAllTextAsync(mashdSource);
        TestPipeline.Run(content);

        var data = (await AdapterFactory.CreateAdapter("csv", new Dictionary<string, string>
        {
            { "source", outputSource },
            { "delimiter", "," }
        }).ReadAsync()).ToArray();
        
        var firstRow = data.First();
        
        // Assert
        Assert.NotEmpty(data);
        Assert.Equal(4, firstRow.Count);
        
        var ticks = Stopwatch.GetTimestamp() - start;
        var elapsedTime = ticks/10000;
        testOutputHelper.WriteLine($"Elapsed time for 'Union_Datasets_With_Match_Conditions_And_Transform': {elapsedTime}ms");
    }
}