namespace HtmlClaimsDataImport.Infrastructure.Services
{
    using HtmlClaimsDataImport.Application.Interfaces;
    using System.Text.Json;
    using System.Text.Json.Nodes;

    public class ConfigService : IConfigService
    {
        public Task<byte[]> ReadConfigAsync(string tmpdir, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(tmpdir);
            var configPath = Path.Combine(tmpdir, "ClaimsDataImportConfig.json");
            if (!File.Exists(configPath))
            {
                // Return empty JSON document
                return Task.FromResult(System.Text.Encoding.UTF8.GetBytes("{}\n"));
            }

            var bytes = File.ReadAllBytes(configPath);
            return Task.FromResult(bytes);
        }

        public Task<bool> SaveMappingAsync(string tmpdir, string outputColumn, string importColumn, CancellationToken cancellationToken = default)
        {
            try
            {
                Directory.CreateDirectory(tmpdir);
                var configPath = Path.Combine(tmpdir, "ClaimsDataImportConfig.json");

                JsonObject root;
                if (File.Exists(configPath))
                {
                    var json = File.ReadAllText(configPath);
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
                File.WriteAllText(configPath, updated);
                return Task.FromResult(true);
            }
            catch
            {
                return Task.FromResult(false);
            }
        }
    }
}

