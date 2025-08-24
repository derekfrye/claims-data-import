using System.Text;
using AngleSharp.Html.Dom;
using Microsoft.AspNetCore.Mvc.Testing;

namespace HtmlClaimsDataImport.Tests;

[Collection("WebApp")]
public class PreviewPage_Clear(WebApplicationFactory<Program> factory)
{
    private readonly WebApplicationFactory<Program> _factory = factory;

    private static Task<string> GetAntiForgeryTokenAsync(HttpClient client) => TestHtmlHelpers.GetAntiForgeryTokenAsync(client);

    private static async Task<(string responseHtml, string tmpdir, string fileName)> UploadTestFileAsync(HttpClient client, string content)
    {
        var token = await GetAntiForgeryTokenAsync(client);
        var fileBytes = Encoding.UTF8.GetBytes(content);

        var tmpdir = Path.Combine(Path.GetTempPath(), $"test-session-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tmpdir);

        using var formData = new MultipartFormDataContent
        {
            { new StringContent("filename"), "fileType" },
            { new StringContent(token), "__RequestVerificationToken" },
            { new StringContent(tmpdir), "tmpdir" },
            { new ByteArrayContent(fileBytes), "uploadedFile", "clear-test.csv" }
        };

        var response = await client.PostAsync("/ClaimsDataImporter?handler=FileUpload", formData);
        response.EnsureSuccessStatusCode();
        var responseHtml = await response.Content.ReadAsStringAsync();
        return (responseHtml, tmpdir, "clear-test.csv");
    }

    private static async Task<System.Text.Json.JsonElement> LoadDataAsync(HttpClient client, string tmpdir, string csvFileName)
    {
        var token = await GetAntiForgeryTokenAsync(client);
        var formData = new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("tmpdir", tmpdir),
            new KeyValuePair<string, string>("fileName", csvFileName),
            new KeyValuePair<string, string>("jsonPath", "default"),
            new KeyValuePair<string, string>("databasePath", "default"),
            new KeyValuePair<string, string>("__RequestVerificationToken", token)
        ]);

        var response = await client.PostAsync("/ClaimsDataImporter?handler=LoadData", formData);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    private static async Task<string> GetPreviewAsync(HttpClient client, string tmpdir, int mappingStep = 0, string selectedColumn = "")
    {
        var token = await GetAntiForgeryTokenAsync(client);
        var formData = new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("tmpdir", tmpdir),
            new KeyValuePair<string, string>("mappingStep", mappingStep.ToString()),
            new KeyValuePair<string, string>("selectedColumn", selectedColumn),
            new KeyValuePair<string, string>("__RequestVerificationToken", token)
        ]);
        var response = await client.PostAsync("/ClaimsDataImporter?handler=Preview", formData);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    private static async Task SaveMappingAsync(HttpClient client, string tmpdir, string outputColumn, string importColumn)
    {
        var token = await GetAntiForgeryTokenAsync(client);
        var formData = new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("tmpdir", tmpdir),
            new KeyValuePair<string, string>("outputColumn", outputColumn),
            new KeyValuePair<string, string>("importColumn", importColumn),
            new KeyValuePair<string, string>("__RequestVerificationToken", token)
        ]);
        var response = await client.PostAsync("/ClaimsDataImporter?handler=SaveMapping", formData);
        response.EnsureSuccessStatusCode();
        _ = await response.Content.ReadAsStringAsync();
    }

    private static async Task ClearMappingAsync(HttpClient client, string tmpdir, string outputColumn)
    {
        var token = await GetAntiForgeryTokenAsync(client);
        var formData = new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("tmpdir", tmpdir),
            new KeyValuePair<string, string>("outputColumn", outputColumn),
            new KeyValuePair<string, string>("__RequestVerificationToken", token)
        ]);
        var response = await client.PostAsync("/ClaimsDataImporter?handler=ClearMapping", formData);
        response.EnsureSuccessStatusCode();
        _ = await response.Content.ReadAsStringAsync();
    }

    private static (IReadOnlyList<string> claimsColumns, Dictionary<string, string> mappings, int step) ExtractSidebarState(IHtmlDocument document)
    {
        var script = document.Scripts.LastOrDefault();
        Assert.NotNull(script);
        var code = script!.TextContent;

        static string ExtractJson(string s, char open, char close)
        {
            int start = s.IndexOf(open);
            Assert.True(start >= 0, $"Could not find '{open}' in script");
            int depth = 0;
            for (int i = start; i < s.Length; i++)
            {
                if (s[i] == open) depth++;
                else if (s[i] == close)
                {
                    depth--;
                    if (depth == 0)
                        return s.Substring(start, i - start + 1);
                }
            }
            throw new InvalidOperationException("Unbalanced JSON extraction");
        }

        var arrayJson = ExtractJson(code, '[', ']');
        var afterArray = code[(code.IndexOf(arrayJson, StringComparison.Ordinal) + arrayJson.Length)..];
        var objectJson = ExtractJson(afterArray, '{', '}');

        var claimsColumns = System.Text.Json.JsonSerializer.Deserialize<List<string>>(arrayJson) ?? [];
        var mappings = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(objectJson) ?? new();
        var tail = afterArray[(afterArray.IndexOf(objectJson, StringComparison.Ordinal) + objectJson.Length)..];
        var stepMatch = System.Text.RegularExpressions.Regex.Match(tail, @"(\d+)");
        Assert.True(stepMatch.Success, "Failed to extract step from script");
        var step = int.Parse(stepMatch.Groups[1].Value);
        return (claimsColumns, mappings, step);
    }

    [Fact]
    public async Task Preview_ClearMapping_WorksAndDisablesHighlight()
    {
        using var client = _factory.CreateClient();

        var csvContent = "ClaimID,Amount,Date,Description\\n1,100.50,2024-01-15,Medical claim\\n2,250.75,2024-01-16,Dental claim";
        var (_, tmpdir, fileName) = await UploadTestFileAsync(client, csvContent);
        var loadJson = await LoadDataAsync(client, tmpdir, fileName);
        Assert.True(loadJson.GetProperty("success").GetBoolean());

        // Step 0: initial preview, no selection -> Clear disabled
        var initialPreviewHtml = await GetPreviewAsync(client, tmpdir, 0, "");
        var initialDoc = await TestHtmlHelpers.ParseDocumentAsync(initialPreviewHtml);
        var clearBtn0 = initialDoc.QuerySelector("#clearBtn") as IHtmlButtonElement;
        Assert.NotNull(clearBtn0);
        Assert.True(clearBtn0!.HasAttribute("disabled"));

        // Select 'date' column -> Clear enabled and column highlighted
        var selectedPreviewHtml = await GetPreviewAsync(client, tmpdir, 0, "date");
        var selectedDoc = await TestHtmlHelpers.ParseDocumentAsync(selectedPreviewHtml);
        var clearBtn1 = selectedDoc.QuerySelector("#clearBtn") as IHtmlButtonElement;
        Assert.NotNull(clearBtn1);
        Assert.False(clearBtn1!.HasAttribute("disabled"));
        Assert.NotNull(selectedDoc.QuerySelector("th.selected-column-header"));

        // Save mapping (simulate Next) for current claims column
        var (claimsCols, _, _) = ExtractSidebarState(selectedDoc);
        var outputColumn = claimsCols[0];
        await SaveMappingAsync(client, tmpdir, outputColumn, "date");

        // Revisit step 0 (no explicit selection) -> should auto-highlight from saved mapping and Clear enabled
        var revisitHtml = await GetPreviewAsync(client, tmpdir, 0, "");
        var revisitDoc = await TestHtmlHelpers.ParseDocumentAsync(revisitHtml);
        Assert.NotNull(revisitDoc.QuerySelector("th.selected-column-header"));
        var clearBtn2 = revisitDoc.QuerySelector("#clearBtn") as IHtmlButtonElement;
        Assert.NotNull(clearBtn2);
        Assert.False(clearBtn2!.HasAttribute("disabled"));

        // Clear mapping -> reload step 0 and verify no highlight and Clear disabled
        await ClearMappingAsync(client, tmpdir, outputColumn);
        var afterClearHtml = await GetPreviewAsync(client, tmpdir, 0, "");
        var afterClearDoc = await TestHtmlHelpers.ParseDocumentAsync(afterClearHtml);
        Assert.Null(afterClearDoc.QuerySelector("th.selected-column-header"));
        var clearBtn3 = afterClearDoc.QuerySelector("#clearBtn") as IHtmlButtonElement;
        Assert.NotNull(clearBtn3);
        Assert.True(clearBtn3!.HasAttribute("disabled"));
    }
}
