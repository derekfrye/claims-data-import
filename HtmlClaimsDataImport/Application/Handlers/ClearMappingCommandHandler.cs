namespace HtmlClaimsDataImport.Application.Handlers;

using HtmlClaimsDataImport.Application.Commands;
using Mediator;
using System.Text.Json;
using System.Text.Json.Nodes;

public class ClearMappingCommandHandler : IRequestHandler<ClearMappingCommand, bool>
{
    public ValueTask<bool> Handle(ClearMappingCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.tmpDir) || string.IsNullOrWhiteSpace(request.outputColumn))
            {
                return ValueTask.FromResult(false);
            }

            Directory.CreateDirectory(request.tmpDir);
            var configPath = Path.Combine(request.tmpDir, "ClaimsDataImportConfig.json");
            JsonObject root;
            if (File.Exists(configPath))
            {
                var json = File.ReadAllText(configPath);
                root = JsonNode.Parse(json) as JsonObject ?? new JsonObject();
            }
            else
            {
                root = new JsonObject();
            }

            var arr = root["translationMapping"] as JsonArray;
            if (arr is null)
            {
                return ValueTask.FromResult(true); // nothing to clear
            }

            var toRemove = new List<JsonNode?>();
            foreach (var node in arr)
            {
                if (node is JsonObject obj)
                {
                    var output = (string?)obj["outputColumn"];
                    if (string.Equals(output, request.outputColumn, StringComparison.Ordinal))
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
            File.WriteAllText(configPath, updated);
            return ValueTask.FromResult(true);
        }
        catch
        {
            return ValueTask.FromResult(false);
        }
    }
}
