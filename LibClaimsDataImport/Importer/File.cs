using Sylvan.Data.Csv;
using Microsoft.Data.Sqlite;
using System.Data;

namespace LibClaimsDataImport.Importer;

public class File
{
    private readonly StreamReader _streamReader;
    private readonly FileSpec _fileSpec;
    private CsvDataReader? _csvReader;

    public File(StreamReader streamReader, FileSpec fileSpec)
    {
        _streamReader = streamReader ?? throw new ArgumentNullException(nameof(streamReader));
        _fileSpec = fileSpec ?? throw new ArgumentNullException(nameof(fileSpec));
    }

    public static File New(StreamReader streamReader, FileSpec fileSpec)
    {
        return new File(streamReader, fileSpec);
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

        // Create connection string
        var connectionString = $"Data Source={filename}";

        using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();

        // Check if table exists
        var tableExistsCommand = connection.CreateCommand();
        tableExistsCommand.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name=$table";
        tableExistsCommand.Parameters.AddWithValue("$table", table);
        
        var tableExists = await tableExistsCommand.ExecuteScalarAsync();
        if (tableExists == null)
        {
            throw new InvalidOperationException($"Table '{table}' does not exist in database '{filename}'");
        }

        // Reset the stream reader to the beginning
        _streamReader.BaseStream.Seek(0, SeekOrigin.Begin);
        _streamReader.DiscardBufferedData();

        // Create new CSV reader
        var csvOptions = new CsvDataReaderOptions
        {
            HasHeaders = true
        };

        _csvReader = CsvDataReader.Create(_streamReader, csvOptions);

        // Prepare bulk insert
        using var transaction = connection.BeginTransaction();
        
        try
        {
            // Build the INSERT statement using the column names from ColumnTypes dictionary
            var columnNames = string.Join(", ", _fileSpec.ColumnTypes.Keys.Select(name => $"[{name}]"));
            var parameterNames = string.Join(", ", _fileSpec.ColumnTypes.Keys.Select((_, index) => $"${index}"));
            var insertSql = $"INSERT INTO [{table}] ({columnNames}) VALUES ({parameterNames})";

            var insertCommand = connection.CreateCommand();
            insertCommand.CommandText = insertSql;
            insertCommand.Transaction = transaction;

            // Add parameters
            for (int i = 0; i < _fileSpec.ColumnTypes.Count; i++)
            {
                insertCommand.Parameters.Add(new SqliteParameter($"${i}", DbType.Object));
            }

            // Read and insert data
            while (await _csvReader.ReadAsync())
            {
                for (int i = 0; i < _fileSpec.ColumnTypes.Count; i++)
                {
                    var value = _csvReader.IsDBNull(i) ? DBNull.Value : _csvReader.GetValue(i);
                    insertCommand.Parameters[i].Value = value;
                }

                await insertCommand.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
