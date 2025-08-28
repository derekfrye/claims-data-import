namespace HtmlClaimsDataImport.Infrastructure.Services
{
    using HtmlClaimsDataImport.Application.Interfaces;
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Nodes;

    public class ConfigService : IConfigService
    {
        private static string GetConfigPath(string tmpdir) => Path.Combine(tmpdir, "ClaimsDataImportConfig.json");

        private static void MigrateLegacyKeys(JsonObject root)
        {
            if (root.ContainsKey("columnMappings") && !root.ContainsKey("stagingColumnMappings"))
            {
                root["stagingColumnMappings"] = root["columnMappings"];
                root.Remove("columnMappings");
            }
        }

        private static JsonArray GetOrCreateTranslationArray(JsonObject root)
        {
            var arr = root["translationMapping"] as JsonArray;
            if (arr is null)
            {
                arr = new JsonArray();
                root["translationMapping"] = arr;
            }
            return arr;
        }

        private static void RemoveMappingsForOutput(JsonArray arr, string outputColumn)
        {
            var toRemove = new List<JsonNode?>();
            foreach (var node in arr)
            {
                if (node is JsonObject obj)
                {
                    var output = (string?)obj["outputColumn"];
                    if (string.Equals(output, outputColumn, StringComparison.Ordinal))
                    {
                        toRemove.Add(node);
                    }
                }
            }
            foreach (var n in toRemove)
            {
                arr.Remove(n);
            }
        }

        private static void AddMapping(JsonArray arr, string outputColumn, string importColumn)
        {
            arr.Add(new JsonObject
            {
                ["inputColumn"] = importColumn,
                ["outputColumn"] = outputColumn,
                ["translationSql"] = string.Empty,
            });
        }

        private static async Task<JsonObject> ReadRootAsync(string configPath, CancellationToken cancellationToken)
        {
            if (File.Exists(configPath))
            {
                var json = await File.ReadAllTextAsync(configPath, cancellationToken).ConfigureAwait(false);
                return JsonNode.Parse(json) as JsonObject ?? new JsonObject();
            }
            return new JsonObject();
        }

        private static async Task WriteRootAsync(string configPath, JsonObject root, CancellationToken cancellationToken)
        {
            var updated = root.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(configPath, updated, cancellationToken).ConfigureAwait(false);
        }

        public async Task<byte[]> ReadConfigAsync(string tmpdir, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(tmpdir);
            var configPath = GetConfigPath(tmpdir);
            if (!File.Exists(configPath))
            {
                return Encoding.UTF8.GetBytes("{}\n");
            }

            var bytes = await File.ReadAllBytesAsync(configPath, cancellationToken).ConfigureAwait(false);
            return bytes;
        }

        public async Task<bool> SaveMappingAsync(string tmpdir, string outputColumn, string importColumn, CancellationToken cancellationToken = default)
        {
            try
            {
                Directory.CreateDirectory(tmpdir);
                var configPath = GetConfigPath(tmpdir);
                var root = await ReadRootAsync(configPath, cancellationToken).ConfigureAwait(false);
                MigrateLegacyKeys(root);
                var arr = GetOrCreateTranslationArray(root);
                RemoveMappingsForOutput(arr, outputColumn);
                AddMapping(arr, outputColumn, importColumn);
                await WriteRootAsync(configPath, root, cancellationToken).ConfigureAwait(false);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> ClearMappingAsync(string tmpdir, string outputColumn, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(tmpdir) || string.IsNullOrWhiteSpace(outputColumn))
                {
                    return false;
                }

                Directory.CreateDirectory(tmpdir);
                var configPath = Path.Combine(tmpdir, "ClaimsDataImportConfig.json");

                JsonObject root;
                if (File.Exists(configPath))
                {
                    var json = await File.ReadAllTextAsync(configPath, cancellationToken).ConfigureAwait(false);
                    root = JsonNode.Parse(json) as JsonObject ?? [];
                }
                else
                {
                    root = [];
                }

                var arr = root["translationMapping"] as JsonArray;
                if (arr is null)
                {
                    return true; // nothing to clear
                }

                var toRemove = new List<JsonNode?>();
                foreach (var node in arr)
                {
                    if (node is JsonObject obj)
                    {
                        var output = (string?)obj["outputColumn"];
                        if (string.Equals(output, outputColumn, StringComparison.Ordinal))
                        {
                            toRemove.Add(node);
                        }
                    }
                }
                foreach (var n in toRemove)
                {
                    arr.Remove(n);
                }

                var updated = root.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(configPath, updated, cancellationToken).ConfigureAwait(false);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
