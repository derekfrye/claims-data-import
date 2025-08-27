namespace HtmlClaimsDataImport.Infrastructure.Services
{
    using HtmlClaimsDataImport.Application.Interfaces;
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Nodes;

    public class ConfigService : IConfigService
    {
        public async Task<byte[]> ReadConfigAsync(string tmpdir, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(tmpdir);
            var configPath = Path.Combine(tmpdir, "ClaimsDataImportConfig.json");
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

                // Migrate legacy property name if present
                if (root.ContainsKey("columnMappings") && !root.ContainsKey("stagingColumnMappings"))
                {
                    root["stagingColumnMappings"] = root["columnMappings"];
                    root.Remove("columnMappings");
                }

                // Ensure translationMapping array exists
                var arr = root["translationMapping"] as JsonArray;
                if (arr is null)
                {
                    arr = [];
                    root["translationMapping"] = arr;
                }

                // Remove existing entry for the same outputColumn
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

                // Add/append new mapping entry
                arr.Add(new JsonObject
                {
                    ["inputColumn"] = importColumn,
                    ["outputColumn"] = outputColumn,
                    ["translationSql"] = string.Empty,
                });

                var updated = root.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(configPath, updated, cancellationToken).ConfigureAwait(false);
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
