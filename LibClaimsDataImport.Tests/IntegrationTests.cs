using System.Diagnostics;
using System.Reflection;
using Microsoft.Data.Sqlite;

namespace LibClaimsDataImport.Tests;

public class IntegrationTests
{
    [Fact]
    public async Task CmdClaimsDataImport_EndToEndTest_WithInMemoryDatabase()
    {
        // Arrange
        var testDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        var projectDirectory = Path.GetFullPath(Path.Combine(testDirectory, "..", "..", ".."));
        var csvFilePath = Path.Combine(projectDirectory, "data.csv");
        
        // Verify test data file exists
        Assert.True(File.Exists(csvFilePath), $"Test data file not found at: {csvFilePath}");

        // Note: The production code will create the table automatically based on configuration

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
                ? $"\"{executablePath}\" --database \":memory:\" --table claims_data --filename \"{csvFilePath}\""
                : $"--database \":memory:\" --table claims_data --filename \"{csvFilePath}\"",
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
    }
}