namespace HtmlClaimsDataImport.Infrastructure.Services
{
    using HtmlClaimsDataImport.Application.Interfaces;
    using HtmlClaimsDataImport.Application.Queries.Dtos;
    using Microsoft.Data.Sqlite;

    public class PreviewService : IPreviewService
    {
        public async Task<PreviewDataDto> GetPreviewDataAsync(string tmpdir, int mappingStep, string selectedColumn)
        {
            var model = new PreviewDataDto
            {
                CurrentMappingStep = mappingStep,
                SelectedImportColumn = selectedColumn,
            };

            // Check if working_db.db exists
            string workingDbPath = Path.Combine(tmpdir, "working_db.db");
            if (!File.Exists(workingDbPath))
            {
                model.StatusMessage = "no working_db.db detected; re-run load.";
                model.IsPreviewAvailable = false;
                return model;
            }

            try
            {
                using var connection = new SqliteConnection($"Data Source={workingDbPath}");
                await connection.OpenAsync().ConfigureAwait(false);

                var importTable = await this.GetLatestImportTableAsync(connection).ConfigureAwait(false);
                if (string.IsNullOrEmpty(importTable))
                {
                    model.StatusMessage = "no import table found; re-run load.";
                    model.IsPreviewAvailable = false;
                    return model;
                }

                await this.LoadImportColumnsAsync(connection, importTable!, model).ConfigureAwait(false);
                await this.LoadClaimsColumnsAsync(connection, model).ConfigureAwait(false);
                await this.LoadPreviewRowsAsync(connection, importTable!, model).ConfigureAwait(false);

                model.StatusMessage = $"Preview loaded: {model.PreviewRows.Count} rows in {importTable}";
                model.IsPreviewAvailable = true;
            }
            catch (SqliteException)
            {
                model.StatusMessage = "working_db.db exists but is not a sqlite db";
                model.IsPreviewAvailable = false;
            }

            return model;
        }

        private static async Task<string?> GetScalarStringAsync(SqliteCommand command)
        {
            using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
            if (await reader.ReadAsync().ConfigureAwait(false))
            {
                return reader.GetString(0);
            }
            return null;
        }

        private async Task<string?> GetLatestImportTableAsync(SqliteConnection connection)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name LIKE 'claims_import_%' ORDER BY name DESC LIMIT 1;";
            return await GetScalarStringAsync(command).ConfigureAwait(false);
        }

        private async Task LoadImportColumnsAsync(SqliteConnection connection, string importTable, PreviewDataDto model)
        {
            using var command = connection.CreateCommand();
            command.CommandText = $"PRAGMA table_info({importTable});";
            using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                model.ImportColumns.Add(reader.GetString(1));
            }
        }

        private async Task LoadClaimsColumnsAsync(SqliteConnection connection, PreviewDataDto model)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='claims';";
            var claimsTableExists = await command.ExecuteScalarAsync().ConfigureAwait(false) != null;
            if (claimsTableExists)
            {
                command.CommandText = "PRAGMA table_info(claims);";
                using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
                while (await reader.ReadAsync().ConfigureAwait(false))
                {
                    model.ClaimsColumns.Add(reader.GetString(1));
                }
            }
            else
            {
                foreach (var c in new[] { "id", "amount", "date", "description", "category" })
                {
                    model.ClaimsColumns.Add(c);
                }
            }
        }

        private async Task LoadPreviewRowsAsync(SqliteConnection connection, string importTable, PreviewDataDto model)
        {
            var columnList = string.Join(", ", model.ImportColumns.Select(c => $"[{c}]"));
            using var command = connection.CreateCommand();
            command.CommandText = $"SELECT {columnList} FROM {importTable} LIMIT 10;";
            using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                var row = new Dictionary<string, string>(StringComparer.Ordinal);
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var columnName = model.ImportColumns[i];
                    row[columnName] = reader.GetValue(i)?.ToString() ?? string.Empty;
                }
                model.PreviewRows.Add(row);
            }
        }
    }
}
