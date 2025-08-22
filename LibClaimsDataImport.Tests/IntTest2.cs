using System.Diagnostics;
using System.Reflection;
using Microsoft.Data.Sqlite;

namespace LibClaimsDataImport.Tests;

public class IntTest2
{
    [Fact]
    public async Task CmdClaimsDataImport_IntTest2_WithAutoDetection()
    {
        // Arrange
        var testDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        var projectDirectory = Path.GetFullPath(Path.Combine(testDirectory, "..", "..", ".."));
        var csvFilePath = Path.Combine(projectDirectory, "IntTest_datafile2.txt");
        var configFilePath = Path.Combine(projectDirectory, "IntTest2.json");
        
        // Create temporary database file for the test
        var tempFile = Path.GetTempFileName();
        var tempDbPath = Path.ChangeExtension(tempFile, $"_inttest2_{Guid.NewGuid()}.db");
        File.Delete(tempFile); // Delete the temp file so we can use the path for SQLite
        
        try
        {
            // Verify test data file exists
            Assert.True(File.Exists(csvFilePath), $"Test data file not found at: {csvFilePath}");
            Assert.True(File.Exists(configFilePath), $"Config file not found at: {configFilePath}");

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
                    ? $"\"{executablePath}\" --database \"{tempDbPath}\" --table drug_claims --filename \"{csvFilePath}\" --config \"{configFilePath}\""
                    : $"--database \"{tempDbPath}\" --table drug_claims --filename \"{csvFilePath}\" --config \"{configFilePath}\"",
                WorkingDirectory = executableDirectory,
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

        // Test: Sum of 'drug_awp_amt' field should equal 5408.96
        var sumCommand = connection.CreateCommand();
        sumCommand.CommandText = "SELECT SUM(drug_awp_amt) FROM drug_claims";
        var sumResult = await sumCommand.ExecuteScalarAsync();
        var actualSum = Convert.ToDecimal(sumResult);
        Assert.Equal(5408.96m, actualSum);

        // Additional validation: Verify we have the expected number of rows
        var countCommand = connection.CreateCommand();
        countCommand.CommandText = "SELECT COUNT(*) FROM drug_claims";
        var countResult = await countCommand.ExecuteScalarAsync();
        var actualCount = Convert.ToInt32(countResult);
        Assert.Equal(10, actualCount); // Should match the number of data rows in the file

        // Verify the table schema was created with auto-detection
        var schemaCommand = connection.CreateCommand();
        schemaCommand.CommandText = "SELECT sql FROM sqlite_master WHERE type='table' AND name='drug_claims'";
        var schemaResult = await schemaCommand.ExecuteScalarAsync();
        var schema = schemaResult?.ToString();
        
        Assert.NotNull(schema);
        Assert.Contains("[recid] INTEGER PRIMARY KEY AUTOINCREMENT", schema); // Auto-generated ID column
        Assert.Contains("[drug_awp_amt]", schema); // Sanitized column name
        Assert.Contains("REAL", schema); // Should detect money values as REAL/decimal
    }
}
