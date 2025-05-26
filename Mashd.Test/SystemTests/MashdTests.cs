using System.Diagnostics;
using Mashd.Test.Fixtures;
using Mashd.Test.IntegrationTests;
using Xunit.Abstractions;

namespace Mashd.Test.SystemTests;

public class MashdTests(ITestOutputHelper testOutputHelper, SystemCsvFixture csv, SystemPostgreSqlFixture db, SystemMashdFixture mashd) 
    : IClassFixture<SystemCsvFixture>, IClassFixture<SystemPostgreSqlFixture>, IClassFixture<SystemMashdFixture>
{
    [Fact]
    public void Join_Datasets_With_Match_Conditions()
    {
        long sw = Stopwatch.GetTimestamp();
        
        // Arrange
        var leftSource = db.ConnectionString;
        var rightSource = csv.SourceFilePath;
        var outputSource = csv.DestinationFilePath;
        var mashdSource = mashd.GenerateJoinMashdWithMatchConditions(leftSource, rightSource, outputSource);
        
        // Act
        var content = File.ReadAllText(mashdSource);
        TestPipeline.Run(content);
        
        // Assert
        
        // TODO: Validate output file
        
        var elapsedTime = Stopwatch.GetTimestamp() - sw;
        testOutputHelper.WriteLine($"Elapsed time for 'Join_Datasets_With_Match_Conditions': {elapsedTime}ms");
    }

    [Fact]
    public void Join_Datasets_With_Transform()
    {
        long sw = Stopwatch.GetTimestamp();
        
        // Arrange
        var leftSource = db.ConnectionString;
        var rightSource = csv.SourceFilePath;
        var outputSource = csv.DestinationFilePath;
        var mashdSource = mashd.GenerateJoinMashdWithMatchConditions(leftSource, rightSource, outputSource);
        
        // Act
        var content = File.ReadAllText(mashdSource);
        TestPipeline.Run(content);
        
        // Assert
        
        // TODO: Validate output file
        
        var elapsedTime = Stopwatch.GetTimestamp() - sw;
        testOutputHelper.WriteLine($"Elapsed time for 'Join_Datasets_With_Transform': {elapsedTime}ms");
    }

    [Fact]
    public void Join_Datasets_With_Match_Conditions_And_Transform()
    {
        long sw = Stopwatch.GetTimestamp();
        
        // Arrange
        var leftSource = db.ConnectionString;
        var rightSource = csv.SourceFilePath;
        var outputSource = csv.DestinationFilePath;
        var mashdSource = mashd.GenerateJoinMashdWithMatchConditions(leftSource, rightSource, outputSource);
        
        // Act
        var content = File.ReadAllText(mashdSource);
        TestPipeline.Run(content);
        
        // Assert
        
        // TODO: Validate output file
        
        var elapsedTime = Stopwatch.GetTimestamp() - sw;
        testOutputHelper.WriteLine($"Elapsed time for 'Join_Datasets_With_Match_Conditions_And_Transform': {elapsedTime}ms");
    }
    
    [Fact]
    public void Union_Datasets_With_Match_Conditions()
    {
        long sw = Stopwatch.GetTimestamp();
        
        // Arrange
        var leftSource = db.ConnectionString;
        var rightSource = csv.SourceFilePath;
        var outputSource = csv.DestinationFilePath;
        var mashdSource = mashd.GenerateJoinMashdWithMatchConditions(leftSource, rightSource, outputSource);
        
        // Act
        var content = File.ReadAllText(mashdSource);
        TestPipeline.Run(content);
        
        // Assert
        
        // TODO: Validate output file
        
        var elapsedTime = Stopwatch.GetTimestamp() - sw;
        testOutputHelper.WriteLine($"Elapsed time for 'Union_Datasets_With_Match_Conditions': {elapsedTime}ms");
    }

    [Fact]
    public void Union_Datasets_With_Transform()
    {
        long sw = Stopwatch.GetTimestamp();
        
        // Arrange
        var leftSource = db.ConnectionString;
        var rightSource = csv.SourceFilePath;
        var outputSource = csv.DestinationFilePath;
        var mashdSource = mashd.GenerateJoinMashdWithMatchConditions(leftSource, rightSource, outputSource);
        
        // Act
        var content = File.ReadAllText(mashdSource);
        TestPipeline.Run(content);
        
        // Assert
        
        // TODO: Validate output file
        
        var elapsedTime = Stopwatch.GetTimestamp() - sw;
        testOutputHelper.WriteLine($"Elapsed time for 'Union_Datasets_With_Transform': {elapsedTime}ms");
    }

    [Fact]
    public void Union_Datasets_With_Match_Conditions_And_Transform()
    {
        long sw = Stopwatch.GetTimestamp();
        
        // Arrange
        var leftSource = db.ConnectionString;
        var rightSource = csv.SourceFilePath;
        var outputSource = csv.DestinationFilePath;
        var mashdSource = mashd.GenerateJoinMashdWithMatchConditions(leftSource, rightSource, outputSource);
        
        // Act
        var content = File.ReadAllText(mashdSource);
        TestPipeline.Run(content);
        
        // Assert
        
        // TODO: Validate output file
        
        var elapsedTime = Stopwatch.GetTimestamp() - sw;
        testOutputHelper.WriteLine($"Elapsed time for 'Union_Datasets_With_Match_Conditions_And_Transform': {elapsedTime}ms");
    }
}