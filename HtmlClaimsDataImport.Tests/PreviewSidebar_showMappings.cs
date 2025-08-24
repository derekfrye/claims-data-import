using System.Text;
using AngleSharp.Html.Dom;
using Microsoft.AspNetCore.Mvc.Testing;

namespace HtmlClaimsDataImport.Tests;

[Collection("WebApp")]
public class PreviewSidebar_ShowMappings(WebApplicationFactory<Program> factory)
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
            { new ByteArrayContent(fileBytes), "uploadedFile", "sidebar-test.csv" }
        };

        var response = await client.PostAsync("/ClaimsDataImporter?handler=FileUpload", formData);
        response.EnsureSuccessStatusCode();
        var responseHtml = await response.Content.ReadAsStringAsync();
        return (responseHtml, tmpdir, "sidebar-test.csv");
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

    private static (IReadOnlyList<string> claimsColumns, Dictionary<string, string> mappings, int step) ExtractSidebarState(IHtmlDocument document)
    {
        // The preview partial includes an inline script that calls:
        // window.updatePreviewSidebar(claimsCols, mappings, step)
        var script = document.Scripts.LastOrDefault();
        Assert.NotNull(script);
        var code = script!.TextContent;

        // Naive parse: find first JSON array and first JSON object in the call
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
                    {
                        return s.Substring(start, i - start + 1);
                    }
                }
            }
            throw new InvalidOperationException("Unbalanced JSON extraction");
        }

        var arrayJson = ExtractJson(code, '[', ']');
        // Remove the array part to locate the next JSON (object)
        var afterArray = code[(code.IndexOf(arrayJson, StringComparison.Ordinal) + arrayJson.Length)..];
        var objectJson = ExtractJson(afterArray, '{', '}');

        var claimsColumns = System.Text.Json.JsonSerializer.Deserialize<List<string>>(arrayJson) ?? [];
        var mappings = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(objectJson) ?? new();

        // Extract the step (look for the last comma and parse trailing int)
        var tail = afterArray[(afterArray.IndexOf(objectJson, StringComparison.Ordinal) + objectJson.Length)..];
        var stepMatch = System.Text.RegularExpressions.Regex.Match(tail, @"(\d+)");
        Assert.True(stepMatch.Success, "Failed to extract step from script");
        var step = int.Parse(stepMatch.Groups[1].Value);

        return (claimsColumns, mappings, step);
    }

    [Fact]
    public async Task PreviewSidebar_ShowsClaimsColumns_TracksMappingState()
    {
        using var client = _factory.CreateClient();

        // Use the same CSV content pattern as the AI test
        var csvContent = "ClaimID,Amount,Date,Description\\n1,100.50,2024-01-15,Medical claim\\n2,250.75,2024-01-16,Dental claim";
        var (_, tmpdir, fileName) = await UploadTestFileAsync(client, csvContent);
        var loadJson = await LoadDataAsync(client, tmpdir, fileName);
        Assert.True(loadJson.GetProperty("success").GetBoolean());

        // Initial preview (no selection) -> sidebar state comes from serialized model
        var initialPreviewHtml = await GetPreviewAsync(client, tmpdir, 0, "");
        var initialDoc = await TestHtmlHelpers.ParseDocumentAsync(initialPreviewHtml);

        var (claimsCols, mappings, step) = ExtractSidebarState(initialDoc);
        // Expect the default claims table columns to be listed in order
        var expected = new[] { "txt_field", "dt_field", "char_field", "int_field", "num_field" };
        foreach (var c in expected)
        {
            Assert.Contains(c, claimsCols);
        }
        Assert.Equal(expected.Length, claimsCols.Count);

        // Initially, all should be unmapped => mapping dict empty or no expected keys present (red dots would be visible)
        Assert.True(mappings.Count == 0 || !expected.Any(k => mappings.ContainsKey(k)));

        // Now select the import column 'date' and fetch preview again (still step 0)
        var selectedPreviewHtml = await GetPreviewAsync(client, tmpdir, 0, "date");
        var selectedDoc = await TestHtmlHelpers.ParseDocumentAsync(selectedPreviewHtml);
        var (_, mappingsBeforeSave, _) = ExtractSidebarState(selectedDoc);
        Assert.True(mappingsBeforeSave.Count == 0 || !expected.Any(k => mappingsBeforeSave.ContainsKey(k)));

        // Simulate clicking Next: Save mapping for the active claims column (index 0), then advance to next step
        var outputColumn = claimsCols[0];
        await SaveMappingAsync(client, tmpdir, outputColumn, "date");

        var afterNextHtml = await GetPreviewAsync(client, tmpdir, 1, "");
        var afterNextDoc = await TestHtmlHelpers.ParseDocumentAsync(afterNextHtml);
        var (claimsAfter, mappingsAfter, stepAfter) = ExtractSidebarState(afterNextDoc);

        Assert.Equal(1, stepAfter);
        Assert.Equal(claimsCols, claimsAfter); // columns remain consistent
        Assert.True(mappingsAfter.ContainsKey(outputColumn), "Expected saved mapping to be present for the first claims column");
        Assert.Equal("date", mappingsAfter[outputColumn]);
    }
}

