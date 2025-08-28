using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using HtmlClaimsDataImport.Application.Interfaces;
using HtmlClaimsDataImport.Application.Queries.Dtos;

namespace HtmlClaimsDataImport.Infrastructure.Services
{
    public class AICompletionService : IAICompletionService
    {
        private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

        public async Task<AIResponseDto> CompleteAsync(string tmpdir, string prompt, CancellationToken cancellationToken = default)
        {
            var apiKey = await TryGetOpenAIKeyFromTmpDirAsync(tmpdir, cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            }
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return new AIResponseDto
                {
                    ResponseText = $"[Simulated AI Response]\n\n{prompt}",
                    IsSimulated = true,
                    Provider = "simulated",
                    Model = "local-dev",
                };
            }

            var http = new HttpClient();
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            http.Timeout = TimeSpan.FromSeconds(45);

            var reqBody = new
            {
                model = "gpt-4o-mini",
                messages = new object[]
                {
                    new { role = "user", content = prompt }
                },
                temperature = 0.2,
            };

            var content = new StringContent(JsonSerializer.Serialize(reqBody, JsonOpts), Encoding.UTF8, "application/json");
            using HttpResponseMessage resp = await http.PostAsync("https://api.openai.com/v1/chat/completions", content, cancellationToken).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode)
            {
                var err = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                return new AIResponseDto
                {
                    ResponseText = $"[AI Error {(int)resp.StatusCode}] {err}",
                    IsSimulated = true,
                    Provider = "openai",
                    Model = "gpt-4o-mini",
                };
            }

            using Stream stream = await resp.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using JsonDocument doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
            JsonElement root = doc.RootElement;
            var text = root.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? string.Empty;
            return new AIResponseDto
            {
                ResponseText = text,
                IsSimulated = false,
                Provider = "openai",
                Model = "gpt-4o-mini",
            };
        }

        private static async Task<string> TryGetOpenAIKeyFromTmpDirAsync(string tmpdir, CancellationToken cancellationToken)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(tmpdir) || !Directory.Exists(tmpdir))
                {
                    return string.Empty;
                }
                foreach (var file in Directory.EnumerateFiles(tmpdir, "*.json", SearchOption.TopDirectoryOnly))
                {
                    var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
                    try
                    {
                        using JsonDocument doc = await JsonDocument.ParseAsync(fs, new JsonDocumentOptions { AllowTrailingCommas = true }, cancellationToken).ConfigureAwait(false);
                        JsonElement root = doc.RootElement;
                        if (root.ValueKind == JsonValueKind.Object)
                        {
                            if (TryGetPropertyCaseInsensitive(root, "openai_api_key", out JsonElement keyEl) && keyEl.ValueKind == JsonValueKind.String)
                            {
                                return keyEl.GetString() ?? string.Empty;
                            }
                        }
                    }
                    finally
                    {
                        await fs.DisposeAsync().ConfigureAwait(false);
                    }
                }
            }
            catch
            {
                // ignore and fall back to env/simulated
            }
            return string.Empty;
        }

        private static bool TryGetPropertyCaseInsensitive(JsonElement obj, string name, out JsonElement value)
        {
            if (obj.TryGetProperty(name, out value))
            {
                return true;
            }
            foreach (JsonProperty prop in obj.EnumerateObject())
            {
                if (string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    value = prop.Value;
                    return true;
                }
            }
            value = default;
            return false;
        }
    }
}
