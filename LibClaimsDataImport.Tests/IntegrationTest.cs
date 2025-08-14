using System.Diagnostics;
using System.Reflection;
using Microsoft.Data.Sqlite;

namespace LibClaimsDataImport.Tests;

public class IntegrationTests
{
    [Fact]
    public async Task CmdClaimsDataImport_EndToEndTest_WithTempDatabase()
    {
        // Arrange
        var testDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        var projectDirectory = Path.GetFullPath(Path.Combine(testDirectory, "..", "..", ".."));
        var csvFilePath = Path.Combine(projectDirectory, "data.csv");
        
        // Create temporary database file for the test
        var tempFile = Path.GetTempFileName();
        var tempDbPath = Path.ChangeExtension(tempFile, $"_test_claims_{Guid.NewGuid()}.db");
        File.Delete(tempFile); // Delete the temp file so we can use the path for SQLite
        
        try
        {
            // Verify test data file exists
            Assert.True(File.Exists(csvFilePath), $"Test data file not found at: {csvFilePath}");

        // Get the path to CmdClaimsDataImport executable
        var cmdProjectPath = Path.GetFullPath(Path.Combine(testDirectory, "..", "..", "..", "..", "CmdClaimsDataImport"));
        var executableDirectory = Path.Combine(cmdProjectPath, "bin", "Debug", "net9.0");
        var executablePath = Path.Combine(executableDirectory, "CmdClaimsDataImport.exe");
        
        // On Linux/Mac, use the dll with dotnet
        if (!File.Exists(executablePath))
        {
            executablePath = Path.Combine(executableDirectory, "CmdClaimsDataImport.dll");
        }

        // Act & Assert
        var processStartInfo = new ProcessStartInfo
        {
            FileName = executablePath.EndsWith(".dll") ? "dotnet" : executablePath,
            Arguments = executablePath.EndsWith(".dll") 
                ? $"\"{executablePath}\" --database \"{tempDbPath}\" --table claims_data --filename \"{csvFilePath}\""
                : $"--database \"{tempDbPath}\" --table claims_data --filename \"{csvFilePath}\"",
            WorkingDirectory = executableDirectory, // Set working directory to where config file is located
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = processStartInfo };
        process.Start();

        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        
        await process.WaitForExitAsync();

        // Verify the process completed successfully
        Assert.True(process.ExitCode == 0, $"Process failed with exit code {process.ExitCode}. Error: {error}. Output: {output}");
        
        // Verify expected output messages
        Assert.Contains("Processing CSV file:", output);
        Assert.Contains("Scanning CSV file to determine schema", output);
        Assert.Contains("Detected", output);
        Assert.Contains("columns:", output);
        Assert.Contains("Importing data to database", output);
        Assert.Contains("Import completed successfully!", output);

            // Verify data was actually imported by checking database content
            await ValidateImportedData($"Data Source={tempDbPath}");
        }
        finally
        {
            // Clean up temporary database file
            if (File.Exists(tempDbPath))
            {
                File.Delete(tempDbPath);
            }
        }
    }

    private static async Task ValidateImportedData(string connectionString)
    {
        using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();

        // Test 1: Sum of 'ing' field should equal 3398.37
        var sumCommand = connection.CreateCommand();
        sumCommand.CommandText = "SELECT SUM(ing) FROM claims_data";
        var sumResult = await sumCommand.ExecuteScalarAsync();
        var actualSum = Convert.ToDecimal(sumResult);
        Assert.Equal(3398.37m, actualSum);

        // Test 2: Max of 'fill_dt' field should be 11/24/2025
        var maxCommand = connection.CreateCommand();
        maxCommand.CommandText = "SELECT MAX(fill_dt) FROM claims_data";
        var maxResult = await maxCommand.ExecuteScalarAsync();
        var maxDateString = maxResult?.ToString();
        
        // Parse the date and verify it's 11/24/2025
        Assert.True(DateTime.TryParse(maxDateString, out var maxDate), $"Could not parse max date: {maxDateString}");
        Assert.Equal(new DateTime(2025, 11, 24), maxDate.Date);

        // Additional validation: Verify we have the expected number of rows
        var countCommand = connection.CreateCommand();
        countCommand.CommandText = "SELECT COUNT(*) FROM claims_data";
        var countResult = await countCommand.ExecuteScalarAsync();
        var actualCount = Convert.ToInt32(countResult);
        Assert.Equal(11, actualCount); // Should match the number of rows in data.csv
    }
}