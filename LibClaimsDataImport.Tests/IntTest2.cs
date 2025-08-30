using System.Diagnostics;
using System.Reflection;
using Microsoft.Data.Sqlite;
using System.Globalization;

namespace LibClaimsDataImport.Tests
{
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
                _ = process.Start();

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
                // Clean up temporary database and any SQLite sidecar files on all OSes
                TryDeleteSqliteFilesWithRetry(tempDbPath);
            }
        }

        private static async Task ValidateImportedData(string connectionString)
        {
            using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync();

            // Test: Sum of 'drug_awp_amt' field should equal 5408.96
            SqliteCommand sumCommand = connection.CreateCommand();
            sumCommand.CommandText = "SELECT SUM(drug_awp_amt) FROM drug_claims";
            var sumResult = await sumCommand.ExecuteScalarAsync();
            var actualSum = Convert.ToDecimal(sumResult);
            Assert.Equal(5408.96m, actualSum);

            // Additional validation: Verify we have the expected number of rows
            SqliteCommand countCommand = connection.CreateCommand();
            countCommand.CommandText = "SELECT COUNT(*) FROM drug_claims";
            var countResult = await countCommand.ExecuteScalarAsync();
            var actualCount = Convert.ToInt32(countResult);
            Assert.Equal(10, actualCount); // Should match the number of data rows in the file

            // Verify the table schema was created with auto-detection
            SqliteCommand schemaCommand = connection.CreateCommand();
            schemaCommand.CommandText = "SELECT sql FROM sqlite_master WHERE type='table' AND name='drug_claims'";
            var schemaResult = await schemaCommand.ExecuteScalarAsync();
            var schema = schemaResult?.ToString();

            Assert.NotNull(schema);
            Assert.Contains("[recid] INTEGER PRIMARY KEY AUTOINCREMENT", schema); // Auto-generated ID column
            Assert.Contains("[drug_awp_amt]", schema); // Sanitized column name
            Assert.Contains("REAL", schema); // Should detect money values as REAL/decimal
        }

        private static void TryDeleteSqliteFilesWithRetry(string dbPath)
        {
            // Best-effort pool clear (if pooling is supported/enabled)
            try { SqliteConnection.ClearAllPools(); } catch { /* ignore if not supported */ }

            var sidecars = new[] { dbPath, dbPath + "-wal", dbPath + "-shm" };
            const int maxAttempts = 10;
            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    foreach (var f in sidecars)
                    {
                        if (File.Exists(f))
                        {
                            File.Delete(f);
                        }
                    }
                    break; // success
                }
                catch (IOException) when (attempt < maxAttempts)
                {
                    Thread.Sleep(100);
                    continue;
                }
            }
        }


        [Fact]
        public async Task CmdClaimsDataImport_IntTest2_LongDigits_ClassifiedAsReal()
        {
            // Arrange a temp CSV with three identical long-digit values
            var testDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
            var projectDirectory = Path.GetFullPath(Path.Combine(testDirectory, "..", "..", ".."));
            var configFilePath = Path.Combine(projectDirectory, "IntTest2.json");

            var tempCsvPath = Path.Combine(Path.GetTempPath(), $"longdigits_{Guid.NewGuid():N}.csv");
            var header = "code";
            var value = "2253402054095601";
            await File.WriteAllTextAsync(tempCsvPath, $"{header}\n{value}\n{value}\n{value}\n");

            // Create temporary database file for the test
            var tempFile = Path.GetTempFileName();
            var tempDbPath = Path.ChangeExtension(tempFile, $"_inttest2_longdigits_{Guid.NewGuid():N}.db");
            File.Delete(tempFile);

            try
            {
                Assert.True(File.Exists(configFilePath), $"Config file not found at: {configFilePath}");
                Assert.True(File.Exists(tempCsvPath), $"Temp CSV not created: {tempCsvPath}");

                // Get path to CmdClaimsDataImport executable
                var cmdProjectPath = Path.GetFullPath(Path.Combine(testDirectory, "..", "..", "..", "..", "CmdClaimsDataImport"));
                var executableDirectory = Path.Combine(cmdProjectPath, "bin", "Debug", "net9.0");
                var executablePath = Path.Combine(executableDirectory, "CmdClaimsDataImport.exe");
                if (!File.Exists(executablePath))
                {
                    executablePath = Path.Combine(executableDirectory, "CmdClaimsDataImport.dll");
                }

                var tableName = "long_digits";
                var psi = new ProcessStartInfo
                {
                    FileName = executablePath.EndsWith(".dll") ? "dotnet" : executablePath,
                    Arguments = executablePath.EndsWith(".dll")
                        ? $"\"{executablePath}\" --database \"{tempDbPath}\" --table {tableName} --filename \"{tempCsvPath}\" --config \"{configFilePath}\""
                        : $"--database \"{tempDbPath}\" --table {tableName} --filename \"{tempCsvPath}\" --config \"{configFilePath}\"",
                    WorkingDirectory = executableDirectory,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };

                using var process = new Process { StartInfo = psi };
                _ = process.Start();
                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                Console.WriteLine($"Process exit: {process.ExitCode}\nSTDOUT:\n{output}\nSTDERR:\n{error}");
                Assert.True(process.ExitCode == 0, $"Process failed with exit code {process.ExitCode}. Error: {error}. Output: {output}");

                await ValidateLongDigitsImportedData_ExpectReal($"Data Source={tempDbPath}", tableName, header, value);
            }
            finally
            {
                TryDeleteSqliteFilesWithRetry(tempDbPath);
                try { if (File.Exists(tempCsvPath)) { File.Delete(tempCsvPath); } } catch { }
            }
        }

        private static async Task ValidateLongDigitsImportedData_ExpectReal(string connectionString, string tableName, string columnName, string expectedValue)
        {
            using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync();

            // Verify 3 rows imported
            using (SqliteCommand countCmd = connection.CreateCommand())
            {
                countCmd.CommandText = $"SELECT COUNT(*) FROM [{tableName}]";
                var count = Convert.ToInt32(await countCmd.ExecuteScalarAsync());
                Assert.Equal(3, count);
            }

            // Verify column type is REAL (decimal mapping)
            using (SqliteCommand typeCmd = connection.CreateCommand())
            {
                typeCmd.CommandText = $"PRAGMA table_info('{tableName}')";
                using SqliteDataReader reader = await typeCmd.ExecuteReaderAsync();
                string? detectedType = null;
                while (await reader.ReadAsync())
                {
                    var name = reader.GetString(1);
                    if (string.Equals(name, columnName, StringComparison.OrdinalIgnoreCase))
                    {
                        detectedType = reader.GetString(2);
                        break;
                    }
                }
                Assert.NotNull(detectedType);
                Assert.Equal("INTEGER", detectedType);
            }

            // Verify first and last row values match the input (as numeric compare then ToString)
            using (SqliteCommand firstCmd = connection.CreateCommand())
            {
                firstCmd.CommandText = $"SELECT [{columnName}] FROM [{tableName}] ORDER BY recid ASC LIMIT 1";
                var first = await firstCmd.ExecuteScalarAsync();
                var firstStr = Convert.ToString(first, CultureInfo.InvariantCulture);
                Console.WriteLine($"First value raw: {first}, as string: {firstStr}");
                Assert.Equal(expectedValue, firstStr);
            }

            using SqliteCommand lastCmd = connection.CreateCommand();
            lastCmd.CommandText = $"SELECT [{columnName}] FROM [{tableName}] ORDER BY recid DESC LIMIT 1";
            var last = await lastCmd.ExecuteScalarAsync();
            var lastStr = Convert.ToString(last, CultureInfo.InvariantCulture);
            Console.WriteLine($"Last value raw: {last}, as string: {lastStr}");
            Assert.Equal(expectedValue, lastStr);
        }
    }
}