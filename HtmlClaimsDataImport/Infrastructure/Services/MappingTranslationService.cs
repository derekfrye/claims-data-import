using System.Text;
using HtmlClaimsDataImport.Application.Interfaces;
using HtmlClaimsDataImport.Application.Queries.Dtos;
using Microsoft.Data.Sqlite;

namespace HtmlClaimsDataImport.Infrastructure.Services
{
    public class MappingTranslationService : IMappingTranslationService
    {
        public async Task<MappingTranslationDto> BuildPromptAsync(string tmpdir, int mappingStep, string selectedColumn)
        {
            var dto = new MappingTranslationDto();

            if (string.IsNullOrWhiteSpace(tmpdir) || string.IsNullOrWhiteSpace(selectedColumn))
            {
                dto.ModelPrompt = "missing tmpdir or selected column";
                return dto;
            }

            var workingDbPath = Path.Combine(tmpdir, "working_db.db");
            if (!File.Exists(workingDbPath))
            {
                dto.ModelPrompt = "working_db.db not found; re-run load";
                return dto;
            }

            using var connection = new SqliteConnection($"Data Source={workingDbPath}");
            await connection.OpenAsync().ConfigureAwait(false);

            var importTable = await GetLatestImportTableAsync(connection).ConfigureAwait(false);
            var destColumn = await GetClaimsColumnByIndexAsync(connection, mappingStep).ConfigureAwait(false) ?? $"claims_col_{mappingStep}";

            (string dataType, bool notNull, bool isUnique, bool isPrimaryKey, string checkDescription) srcSchema = await GetColumnSchemaAsync(connection, importTable ?? string.Empty, selectedColumn).ConfigureAwait(false);
            (string dataType, bool notNull, bool isUnique, bool isPrimaryKey, string checkDescription) dstSchema = await GetColumnSchemaAsync(connection, "claims", destColumn).ConfigureAwait(false);
            List<string?> distinctValues = await GetDistinctValuesAsync(connection, importTable ?? string.Empty, selectedColumn, 10).ConfigureAwait(false);
            var valuesCsv = string.Join(", ", distinctValues.Select(v => v is null ? "NULL" : $"\"{v.Replace("\"", "\\\"", StringComparison.Ordinal)}\""));

            dto.ModelPrompt = ComposePrompt(selectedColumn, destColumn, srcSchema, dstSchema, valuesCsv);
            return dto;
        }

        private static string ComposePrompt(
            string selectedColumn,
            string destColumn,
            (string dataType, bool notNull, bool isUnique, bool isPrimaryKey, string checkDescription) srcSchema,
            (string dataType, bool notNull, bool isUnique, bool isPrimaryKey, string checkDescription) dstSchema,
            string valuesCsv)
        {
            var sb = new StringBuilder();
            _ = sb.Append($"Please provide modern sqlite code that could be pasted as-is into a select query, such that it could clearly translate from {selectedColumn} to {destColumn}. ");
            _ = sb.Append($"{selectedColumn} is sqlite data type {srcSchema.dataType}, and {destColumn} is sqlite data type {dstSchema.dataType}. ");
            _ = sb.Append($"Source column constraints: {DescribeConstraints(srcSchema)}. Destination column constraints: {DescribeConstraints(dstSchema)}. ");
            _ = sb.Append($"Here's the first 10 distinct values from {selectedColumn}: {valuesCsv}. ");
            _ = sb.Append($"In your code, refer to the source column as {selectedColumn}. Refer to the destination column as {destColumn}. ");
            _ = sb.Append("You do not need the keyword SELECT, nor do you need any FROM, WHERE, GROUP BY, LIMIT or any clause or keyword that wouldn't be pasted directly into the column-portion of a query's SELECT clause.");
            _ = sb.Append("You do not need a leading or trailing comma.");
            _ = sb.Append("You cannot create views, temporary tables, or any multi-part queries, you can only write code that would be pasted directly into the column-portion of a query's SELECT clause.");
            _ = sb.Append($"You do need to end your code with \"AS {destColumn}\". ");
            _ = sb.Append("Document your code with comments if you think the typical high school student would not understand what a specific line of code is doing. Remember to respond with code that will compile without errors.");
            return sb.ToString();
        }

        private static string DescribeConstraints((string dataType, bool notNull, bool isUnique, bool isPrimaryKey, string checkDescription) s)
        {
            var constraints = new List<string>
            {
                $"nullable: {(s.notNull ? "no" : "yes")}",
                $"unique: {(s.isUnique ? "yes" : "no")}",
            };
            if (s.isPrimaryKey)
            {
                constraints.Add("primary key");
            }
            if (!string.IsNullOrWhiteSpace(s.checkDescription))
            {
                constraints.Add($"checks: {s.checkDescription}");
            }
            return string.Join(", ", constraints);
        }

        private static async Task<string?> GetLatestImportTableAsync(SqliteConnection connection)
        {
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name LIKE 'claims_import_%' ORDER BY name DESC LIMIT 1;";
            using SqliteDataReader reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
            return await reader.ReadAsync().ConfigureAwait(false) ? reader.GetString(0) : null;
        }

        private static async Task<string?> GetClaimsColumnByIndexAsync(SqliteConnection connection, int index)
        {
            using SqliteCommand cmdExists = connection.CreateCommand();
            cmdExists.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='claims';";
            var exists = await cmdExists.ExecuteScalarAsync().ConfigureAwait(false) != null;
            if (!exists)
            {
                return null;
            }

            using SqliteCommand cmd = connection.CreateCommand();
            cmd.CommandText = "PRAGMA table_info(claims);";
            using SqliteDataReader reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
            var i = 0;
            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                if (i == index)
                {
                    return reader.GetString(1);
                }
                i++;
            }
            return null;
        }

        private static async Task<(string dataType, bool notNull, bool isUnique, bool isPrimaryKey, string checkDescription)> GetColumnSchemaAsync(SqliteConnection connection, string tableName, string columnName)
        {
            if (string.IsNullOrWhiteSpace(tableName) || string.IsNullOrWhiteSpace(columnName))
            {
                return ("unknown", false, false, false, string.Empty);
            }

            (var dataType, var notNull, var isPrimaryKey) = await ReadColumnInfoAsync(connection, tableName, columnName).ConfigureAwait(false);
            var isUnique = await IsColumnUniqueAsync(connection, tableName, columnName).ConfigureAwait(false);
            var checkDesc = await GetCheckDescriptionAsync(connection, tableName, columnName).ConfigureAwait(false);

            return (dataType, notNull, isUnique, isPrimaryKey, checkDesc);
        }

        private static async Task<(string dataType, bool notNull, bool isPrimaryKey)> ReadColumnInfoAsync(SqliteConnection connection, string tableName, string columnName)
        {
            var dataType = "unknown";
            var notNull = false;
            var isPrimaryKey = false;

            using SqliteCommand cmd = connection.CreateCommand();
            cmd.CommandText = $"PRAGMA table_info([{tableName}]);";
            using SqliteDataReader reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                var name = reader.GetString(1);
                if (string.Equals(name, columnName, StringComparison.Ordinal))
                {
                    dataType = reader.IsDBNull(2) ? "unknown" : reader.GetString(2);
                    notNull = !reader.IsDBNull(3) && reader.GetInt32(3) == 1;
                    isPrimaryKey = !reader.IsDBNull(5) && reader.GetInt32(5) > 0;
                    break;
                }
            }

            return (dataType, notNull, isPrimaryKey);
        }

        private static async Task<bool> IsColumnUniqueAsync(SqliteConnection connection, string tableName, string columnName)
        {
            using SqliteCommand cmd = connection.CreateCommand();
            cmd.CommandText = $"PRAGMA index_list([{tableName}]);";
            using SqliteDataReader idxReader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
            var uniqueIndexes = new List<string>();
            while (await idxReader.ReadAsync().ConfigureAwait(false))
            {
                var isUniqueIdx = !idxReader.IsDBNull(2) && idxReader.GetBoolean(2);
                if (isUniqueIdx)
                {
                    uniqueIndexes.Add(idxReader.GetString(1));
                }
            }

            foreach (var idxName in uniqueIndexes)
            {
                using SqliteCommand idxInfoCmd = connection.CreateCommand();
                idxInfoCmd.CommandText = $"PRAGMA index_info([{idxName}]);";
                using SqliteDataReader creader = await idxInfoCmd.ExecuteReaderAsync().ConfigureAwait(false);
                while (await creader.ReadAsync().ConfigureAwait(false))
                {
                    var colName = creader.GetString(2);
                    if (string.Equals(colName, columnName, StringComparison.Ordinal))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static async Task<string> GetCheckDescriptionAsync(SqliteConnection connection, string tableName, string columnName)
        {
            using SqliteCommand cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT sql FROM sqlite_master WHERE type='table' AND name=@t;";
            _ = cmd.Parameters.AddWithValue("@t", tableName);
            var sql = (string?)await cmd.ExecuteScalarAsync().ConfigureAwait(false) ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(sql) && sql.Contains("CHECK", StringComparison.OrdinalIgnoreCase))
            {
                var mentionsCol = sql.IndexOf(columnName, StringComparison.OrdinalIgnoreCase) >= 0;
                return mentionsCol ? $"CHECK constraint referencing '{columnName}' present" : "table-level CHECK present";
            }
            return string.Empty;
        }

        private static async Task<List<string?>> GetDistinctValuesAsync(SqliteConnection connection, string tableName, string columnName, int limit)
        {
            var values = new List<string?>();
            if (string.IsNullOrWhiteSpace(tableName) || string.IsNullOrWhiteSpace(columnName))
            {
                return values;
            }
            using SqliteCommand cmd = connection.CreateCommand();
            cmd.CommandText = $"SELECT DISTINCT [{columnName}] FROM [{tableName}] WHERE [{columnName}] IS NOT NULL LIMIT {limit};";
            using SqliteDataReader reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                values.Add(reader.IsDBNull(0) ? null : reader.GetValue(0)?.ToString());
            }
            return values;
        }
    }
}
