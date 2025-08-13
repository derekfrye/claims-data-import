using Sylvan.Data.Csv;
using Microsoft.Data.Sqlite;
using System.Data;

namespace LibClaimsDataImport.Importer;

public class File
{
    private readonly StreamReader _streamReader;
    private readonly FileSpec _fileSpec;
    private readonly ImportConfig _config;
    private CsvDataReader? _csvReader;

    public File(StreamReader streamReader, FileSpec fileSpec, ImportConfig? config = null)
    {
        _streamReader = streamReader ?? throw new ArgumentNullException(nameof(streamReader));
        _fileSpec = fileSpec ?? throw new ArgumentNullException(nameof(fileSpec));
        _config = config ?? ImportConfig.LoadFromFile();
    }

    public static File New(StreamReader streamReader, FileSpec fileSpec, ImportConfig? config = null)
    {
        return new File(streamReader, fileSpec, config);
    }

    public async Task WriteToDb(string filename, string table)
    {
        if (string.IsNullOrWhiteSpace(filename))
            throw new ArgumentException("Filename cannot be null or empty", nameof(filename));
        
        if (string.IsNullOrWhiteSpace(table))
            throw new ArgumentException("Table name cannot be null or empty", nameof(table));

        if (_fileSpec.ColumnTypes.Count == 0)
            throw new InvalidOperationException("FileSpec.Scan() must be called before WriteToDb");

        // Verify the database file is writable
        var directory = Path.GetDirectoryName(filename);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            throw new DirectoryNotFoundException($"Directory does not exist: {directory}");
        }

        // Create connection string with configuration
        var connectionStringBuilder = new SqliteConnectionStringBuilder
        {
            DataSource = filename,
            DefaultTimeout = _config.SqliteSettings.ConnectionSettings.DefaultTimeout,
            ForeignKeys = _config.SqliteSettings.ConnectionSettings.EnableForeignKeys
        };

        using var connection = new SqliteConnection(connectionStringBuilder.ConnectionString);
        await connection.OpenAsync();

        // Apply pragma settings from configuration
        if (!string.IsNullOrEmpty(_config.SqliteSettings.ConnectionSettings.JournalMode))
        {
            var journalCommand = connection.CreateCommand();
            journalCommand.CommandText = $"PRAGMA journal_mode = {_config.SqliteSettings.ConnectionSettings.JournalMode}";
            await journalCommand.ExecuteNonQueryAsync();
        }

        // Apply additional pragma settings
        foreach (var pragma in _config.SqliteSettings.ConnectionSettings.Pragma)
        {
            var pragmaCommand = connection.CreateCommand();
            pragmaCommand.CommandText = $"PRAGMA {pragma.Key} = {pragma.Value}";
            await pragmaCommand.ExecuteNonQueryAsync();
        }

        // Create table if it doesn't exist using configuration
        await _config.CreateTableIfNotExists(connection, table, _fileSpec);

        // Reset the stream reader to the beginning
        _streamReader.BaseStream.Seek(0, SeekOrigin.Begin);
        _streamReader.DiscardBufferedData();

        // Create new CSV reader
        var csvOptions = new CsvDataReaderOptions
        {
            HasHeaders = true
        };

        _csvReader = CsvDataReader.Create(_streamReader, csvOptions);

        // Prepare bulk insert with transaction handling based on configuration
        SqliteTransaction? transaction = null;
        if (_config.SqliteSettings.ImportSettings.EnableTransactions)
        {
            transaction = connection.BeginTransaction();
        }
        
        try
        {
            // Build the INSERT statement using the column names from ColumnTypes dictionary
            var columnNames = string.Join(", ", _fileSpec.ColumnTypes.Keys.Select(name => $"[{name}]"));
            var parameterNames = string.Join(", ", _fileSpec.ColumnTypes.Keys.Select((_, index) => $"${index}"));
            var insertSql = $"INSERT INTO [{table}] ({columnNames}) VALUES ({parameterNames})";

            var insertCommand = connection.CreateCommand();
            insertCommand.CommandText = insertSql;
            if (transaction != null)
            {
                insertCommand.Transaction = transaction;
            }

            // Add parameters
            for (int i = 0; i < _fileSpec.ColumnTypes.Count; i++)
            {
                insertCommand.Parameters.Add(new SqliteParameter($"${i}", DbType.Object));
            }

            // Read and insert data with batch processing
            int rowCount = 0;
            int errorCount = 0;
            while (await _csvReader.ReadAsync())
            {
                try
                {
                    for (int i = 0; i < _fileSpec.ColumnTypes.Count; i++)
                    {
                        if (_csvReader.IsDBNull(i))
                        {
                            insertCommand.Parameters[i].Value = DBNull.Value;
                        }
                        else
                        {
                            var columnName = _fileSpec.ColumnTypes.Keys.ElementAt(i);
                            var columnType = _fileSpec.ColumnTypes[columnName];
                            var rawValue = _csvReader.GetString(i);

                            // Use the centralized data type logic for consistent parsing
                            var parsedValue = DataTypeDetector.ParseValue(rawValue, columnType);
                            insertCommand.Parameters[i].Value = parsedValue;
                        }
                    }

                    await insertCommand.ExecuteNonQueryAsync();
                    rowCount++;

                    // Commit batch if configured batch size is reached
                    if (_config.SqliteSettings.ImportSettings.BatchSize > 0 && rowCount % _config.SqliteSettings.ImportSettings.BatchSize == 0)
                    {
                        if (transaction != null)
                        {
                            await transaction.CommitAsync();
                            transaction = connection.BeginTransaction();
                            insertCommand.Transaction = transaction;
                        }
                    }
                }
                catch (Exception ex)
                {
                    errorCount++;
                    if (!_config.SqliteSettings.ImportSettings.ContinueOnError)
                    {
                        throw new InvalidOperationException($"Error processing row {rowCount + 1}: {ex.Message}", ex);
                    }
                    
                    if (errorCount >= _config.Validation.MaxRowErrors)
                    {
                        throw new InvalidOperationException($"Maximum error count ({_config.Validation.MaxRowErrors}) exceeded at row {rowCount + 1}");
                    }
                    
                    // Log error if continuing on error (basic console logging for now)
                    if (_config.SqliteSettings.ImportSettings.LogLevel.Equals("info", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine($"Warning: Error at row {rowCount + 1}: {ex.Message}");
                    }
                }
            }

            // Final commit
            if (transaction != null)
            {
                await transaction.CommitAsync();
            }
        }
        catch
        {
            if (transaction != null)
            {
                await transaction.RollbackAsync();
            }
            throw;
        }
        finally
        {
            transaction?.Dispose();
        }
    }

}
