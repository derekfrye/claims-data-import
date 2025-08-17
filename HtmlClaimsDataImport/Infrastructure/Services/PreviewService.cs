namespace HtmlClaimsDataImport.Infrastructure.Services
{
    using HtmlClaimsDataImport.Application.Interfaces;
    using HtmlClaimsDataImport.Models;
    using Microsoft.Data.Sqlite;

    public class PreviewService : IPreviewService
    {
        public async Task<PreviewDataModel> GetPreviewDataAsync(string tmpdir, int mappingStep, string selectedColumn)
        {
            var model = new PreviewDataModel
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

            // List tables in the working database to find import table
            string? importTable = null;
            
            try
            {
                using var connection = new SqliteConnection($"Data Source={workingDbPath}");
                await connection.OpenAsync();

                using var command = connection.CreateCommand();
                command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name LIKE 'claims_import_%' ORDER BY name DESC LIMIT 1;";
                
                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        importTable = reader.GetString(0);
                    }
                }

                if (string.IsNullOrEmpty(importTable))
                {
                    model.StatusMessage = "no import table found; re-run load.";
                    model.IsPreviewAvailable = false;
                    return model;
                }

                // Get column names from import table
                command.CommandText = $"PRAGMA table_info({importTable});";
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        model.ImportColumns.Add(reader.GetString(1)); // column name is at index 1
                    }
                }

                // Get column names from claims table (if it exists)
                command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='claims';";
                var claimsTableExists = await command.ExecuteScalarAsync() != null;
                
                if (claimsTableExists)
                {
                    command.CommandText = "PRAGMA table_info(claims);";
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            model.ClaimsColumns.Add(reader.GetString(1)); // column name is at index 1
                        }
                    }
                }
                else
                {
                    // Default claims columns if table doesn't exist
                    model.ClaimsColumns.AddRange(["id", "amount", "date", "description", "category"]);
                }

                // Get first 10 rows of preview data
                var columnList = string.Join(", ", model.ImportColumns.Select(c => $"[{c}]"));
                command.CommandText = $"SELECT {columnList} FROM {importTable} LIMIT 10;";
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var row = new Dictionary<string, string>();
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            var columnName = model.ImportColumns[i];
                            row[columnName] = reader.GetValue(i)?.ToString() ?? "";
                        }
                        model.PreviewRows.Add(row);
                    }
                }

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
    }
}