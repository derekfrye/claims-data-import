namespace LibClaimsDataImport.Importer
{
    using System.Data;
    using Microsoft.Data.Sqlite;
    using Sylvan.Data.Csv;

    /// <summary>
    /// Provides CSV file ingestion and bulk insertion into SQLite.
    /// </summary>
    public class File
    {
        private readonly StreamReader streamReader;
        private readonly FileSpec fileSpec;
        private readonly ImportConfig config;
        private CsvDataReader? csvReader;

        /// <summary>
        /// Initializes a new instance of the <see cref="File"/> class.
        /// </summary>
        /// <param name="streamReader">The source CSV stream reader.</param>
        /// <param name="fileSpec">The file specification containing header and type info.</param>
        /// <param name="config">Optional import configuration; defaults to loading from config file.</param>
        public File(StreamReader streamReader, FileSpec fileSpec, ImportConfig? config = null)
        {
            this.streamReader = streamReader ?? throw new ArgumentNullException(nameof(streamReader));
            this.fileSpec = fileSpec ?? throw new ArgumentNullException(nameof(fileSpec));
            this.config = config ?? ImportConfig.LoadFromFile();
        }

        /// <summary>
        /// Factory method for constructing <see cref="File"/> with optional configuration.
        /// </summary>
        /// <param name="streamReader">The source CSV stream reader.</param>
        /// <param name="fileSpec">The file specification containing header and type info.</param>
        /// <param name="config">Optional import configuration; defaults to loading from config file.</param>
        /// <returns>A configured <see cref="File"/> instance.</returns>
        public static File New(StreamReader streamReader, FileSpec fileSpec, ImportConfig? config = null)
        {
            return new File(streamReader, fileSpec, config);
        }

        /// <summary>
        /// Writes the CSV rows to the specified SQLite database and table.
        /// </summary>
        /// <param name="filename">Path to the SQLite database file.</param>
        /// <param name="table">Destination table name (created if missing).</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task WriteToDb(string filename, string table)
        {
            ValidateInputs(filename, table);
            EnsureDirectoryExists(filename);

            using var connection = CreateConnection(filename);
            await connection.OpenAsync().ConfigureAwait(false);

            await ApplyPragmasAsync(connection).ConfigureAwait(false);
            await this.config.CreateTableIfNotExists(connection, table, this.fileSpec).ConfigureAwait(false);

            PrepareCsvReader();

            SqliteTransaction? transaction = this.config.SqliteSettings.ImportSettings.EnableTransactions
                ? connection.BeginTransaction()
                : null;

            try
            {
                transaction = await BulkInsertAsync(connection, transaction, table).ConfigureAwait(false);
                if (transaction != null)
                {
                    await transaction.CommitAsync().ConfigureAwait(false);
                }
            }
            catch
            {
                if (transaction != null)
                {
                    await transaction.RollbackAsync().ConfigureAwait(false);
                }
                throw;
            }
            finally
            {
                transaction?.Dispose();
            }
        }

        private void ValidateInputs(string filename, string table)
        {
            if (string.IsNullOrWhiteSpace(filename))
            {
                throw new ArgumentException("Filename cannot be null or empty", nameof(filename));
            }
            if (string.IsNullOrWhiteSpace(table))
            {
                throw new ArgumentException("Table name cannot be null or empty", nameof(table));
            }
            if (this.fileSpec.ColumnTypes.Count == 0)
            {
                throw new InvalidOperationException("FileSpec.Scan() must be called before WriteToDb");
            }
        }

        private void EnsureDirectoryExists(string filename)
        {
            var directory = Path.GetDirectoryName(filename);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                throw new DirectoryNotFoundException($"Directory does not exist: {directory}");
            }
        }

        private SqliteConnection CreateConnection(string filename)
        {
            var csb = new SqliteConnectionStringBuilder
            {
                DataSource = filename,
                DefaultTimeout = this.config.SqliteSettings.ConnectionSettings.DefaultTimeout,
                ForeignKeys = this.config.SqliteSettings.ConnectionSettings.EnableForeignKeys,
            };
            return new SqliteConnection(csb.ConnectionString);
        }

        private async Task ApplyPragmasAsync(SqliteConnection connection)
        {
            if (!string.IsNullOrEmpty(this.config.SqliteSettings.ConnectionSettings.JournalMode))
            {
                var journalCommand = connection.CreateCommand();
                journalCommand.CommandText = $"PRAGMA journal_mode = {this.config.SqliteSettings.ConnectionSettings.JournalMode}";
                await journalCommand.ExecuteNonQueryAsync().ConfigureAwait(false);
            }

            foreach (var pragma in this.config.SqliteSettings.ConnectionSettings.Pragma)
            {
                var pragmaCommand = connection.CreateCommand();
                pragmaCommand.CommandText = $"PRAGMA {pragma.Key} = {pragma.Value}";
                await pragmaCommand.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
        }

        private void PrepareCsvReader()
        {
            this.streamReader.BaseStream.Seek(0, SeekOrigin.Begin);
            this.streamReader.DiscardBufferedData();
            var csvOptions = new CsvDataReaderOptions { HasHeaders = true };
            this.csvReader = CsvDataReader.Create(this.streamReader, csvOptions);
        }

        private SqliteCommand CreateInsertCommand(SqliteConnection connection, SqliteTransaction? transaction, string table)
        {
            var columnNames = string.Join(", ", this.fileSpec.ColumnTypes.Keys.Select(name => $"[{name}]"));
            var parameterNames = string.Join(", ", this.fileSpec.ColumnTypes.Keys.Select((_, index) => $"${index}"));
            var insertSql = $"INSERT INTO [{table}] ({columnNames}) VALUES ({parameterNames})";

            var insertCommand = connection.CreateCommand();
            insertCommand.CommandText = insertSql;
            if (transaction != null)
            {
                insertCommand.Transaction = transaction;
            }
            for (int i = 0; i < this.fileSpec.ColumnTypes.Count; i++)
            {
                insertCommand.Parameters.Add(new SqliteParameter($"${i}", DbType.Object));
            }
            return insertCommand;
        }

        private async Task<SqliteTransaction?> BulkInsertAsync(SqliteConnection connection, SqliteTransaction? transaction, string table)
        {
            var insertCommand = CreateInsertCommand(connection, transaction, table);
            var reader = this.csvReader ?? throw new InvalidOperationException("CSV reader is not initialized");
            int rowCount = 0;
            int errorCount = 0;

            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                try
                {
                    for (int i = 0; i < this.fileSpec.ColumnTypes.Count; i++)
                    {
                        if (reader.IsDBNull(i))
                        {
                            insertCommand.Parameters[i].Value = DBNull.Value;
                        }
                        else
                        {
                            var columnName = this.fileSpec.ColumnTypes.Keys.ElementAt(i);
                            var columnType = this.fileSpec.ColumnTypes[columnName];
                            var rawValue = reader.GetString(i);
                            var parsedValue = DataTypeDetector.ParseValue(rawValue, columnType);
                            insertCommand.Parameters[i].Value = parsedValue;
                        }
                    }

                    await insertCommand.ExecuteNonQueryAsync().ConfigureAwait(false);
                    rowCount++;

                    if (this.config.SqliteSettings.ImportSettings.BatchSize > 0 && rowCount % this.config.SqliteSettings.ImportSettings.BatchSize == 0)
                    {
                        if (transaction != null)
                        {
                            await transaction.CommitAsync().ConfigureAwait(false);
                            transaction = connection.BeginTransaction();
                            insertCommand.Transaction = transaction;
                        }
                    }
                }
                catch (Exception ex)
                {
                    errorCount++;
                    if (!this.config.SqliteSettings.ImportSettings.ContinueOnError)
                    {
                        throw new InvalidOperationException($"Error processing row {rowCount + 1}: {ex.Message}", ex);
                    }

                    if (errorCount >= this.config.Validation.MaxRowErrors)
                    {
                        throw new InvalidOperationException($"Maximum error count ({this.config.Validation.MaxRowErrors}) exceeded at row {rowCount + 1}");
                    }

                    if (this.config.SqliteSettings.ImportSettings.LogLevel.Equals("info", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine($"Warning: Error at row {rowCount + 1}: {ex.Message}");
                    }
                }
            }

            return transaction;
        }
    }
}
