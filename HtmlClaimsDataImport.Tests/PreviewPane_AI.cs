using System.Text;
using AngleSharp.Html.Dom;
using Microsoft.AspNetCore.Mvc.Testing;

namespace HtmlClaimsDataImport.Tests;

[Collection("WebApp")]
public class PreviewPane_AI(WebApplicationFactory<Program> factory)
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
            { new ByteArrayContent(fileBytes), "uploadedFile", "ai-test.csv" }
        };

        var response = await client.PostAsync("/ClaimsDataImporter?handler=FileUpload", formData);
        response.EnsureSuccessStatusCode();
        var responseHtml = await response.Content.ReadAsStringAsync();
        return (responseHtml, tmpdir, "ai-test.csv");
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

    private static async Task<string> PostMappingTranslationAsync(HttpClient client, string tmpdir, int mappingStep, string selectedColumn)
    {
        var token = await GetAntiForgeryTokenAsync(client);
        var formData = new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("tmpdir", tmpdir),
            new KeyValuePair<string, string>("mappingStep", mappingStep.ToString()),
            new KeyValuePair<string, string>("selectedColumn", selectedColumn),
            new KeyValuePair<string, string>("__RequestVerificationToken", token)
        ]);
        var response = await client.PostAsync("/ClaimsDataImporter?handler=MappingTranslation", formData);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    [Fact]
    public async Task PreviewPane_AIButton_DisabledUntilColumnSelected_ThenReturnsPrompt()
    {
        using var sessionClient = _factory.CreateClient();

        // Upload file and load data
        var csvContent = "ClaimID,Amount,Date,Description\n1,100.50,2024-01-15,Medical claim\n2,250.75,2024-01-16,Dental claim";
        var (_, tmpdir, fileName) = await UploadTestFileAsync(sessionClient, csvContent);
        var loadJson = await LoadDataAsync(sessionClient, tmpdir, fileName);
        Assert.True(loadJson.GetProperty("success").GetBoolean());

        // Get preview WITHOUT selecting a column yet
        var previewHtmlNoSelection = await GetPreviewAsync(sessionClient, tmpdir, 0, "");
        var docNoSel = await TestHtmlHelpers.ParseDocumentAsync(previewHtmlNoSelection);

        // The AI button should be present but disabled when no column is selected
        var aiBtnDisabled = docNoSel.QuerySelector("#ai-translate-btn") as IHtmlButtonElement;
        Assert.NotNull(aiBtnDisabled);
        Assert.True(aiBtnDisabled!.HasAttribute("disabled"));

        // The model-prompt div should exist and be empty
        var modelPromptEmpty = docNoSel.QuerySelector("#model-prompt");
        Assert.NotNull(modelPromptEmpty);
        Assert.True(string.IsNullOrWhiteSpace(modelPromptEmpty!.InnerHtml));

        // Now select a column (e.g., 'amount') and fetch preview again
        var previewHtmlSelected = await GetPreviewAsync(sessionClient, tmpdir, 0, "amount");
        var docSelected = await TestHtmlHelpers.ParseDocumentAsync(previewHtmlSelected);

        // The AI button should now be enabled
        var aiBtnEnabled = docSelected.QuerySelector("#ai-translate-btn") as IHtmlButtonElement;
        Assert.NotNull(aiBtnEnabled);
        Assert.False(aiBtnEnabled!.HasAttribute("disabled"));

        // Simulate clicking the AI button by posting to MappingTranslation
        var mappingResponseHtml = await PostMappingTranslationAsync(sessionClient, tmpdir, 0, "amount");

        // After the MappingTranslation call, the partial would be injected into #model-prompt by htmx.
        // Validate the returned HTML contains the expected prompt text fragment.
        Assert.Contains("Please provide modern sqlite", mappingResponseHtml, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("translate from amount", mappingResponseHtml, StringComparison.OrdinalIgnoreCase);
    }
}

