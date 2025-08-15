using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace HtmlClaimsDataImport.Tests;

public class FileSelectionIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public FileSelectionIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task PostFileUpload_FilenameType_ReturnsInputFieldAndUploadLog()
    {
        // First, get the page to obtain the anti-forgery token
        var getResponse = await _client.GetAsync("/ClaimsDataImporter");
        getResponse.EnsureSuccessStatusCode();
        var getContent = await getResponse.Content.ReadAsStringAsync();
        
        // Extract the anti-forgery token from the hidden input field
        var tokenStart = getContent.IndexOf("__RequestVerificationToken\" type=\"hidden\" value=\"") + "__RequestVerificationToken\" type=\"hidden\" value=\"".Length;
        var tokenEnd = getContent.IndexOf("\"", tokenStart);
        var token = getContent.Substring(tokenStart, tokenEnd - tokenStart);

        // Create a test CSV file content
        var testFileContent = "Name,Age,City\nJohn,25,New York\nJane,30,Boston";
        var fileBytes = Encoding.UTF8.GetBytes(testFileContent);

        // Prepare multipart form data for file upload
        using var formData = new MultipartFormDataContent();
        formData.Add(new StringContent("filename"), "fileType");
        formData.Add(new StringContent(token), "__RequestVerificationToken");
        formData.Add(new ByteArrayContent(fileBytes), "uploadedFile", "test.csv");

        // Make the POST request to the file upload handler
        var response = await _client.PostAsync("/ClaimsDataImporter?handler=FileUpload", formData);
        
        // Ensure the request was successful
        response.EnsureSuccessStatusCode();
        
        // Get the response content
        var responseContent = await response.Content.ReadAsStringAsync();
        
        // Verify the response contains the expected HTML with temp file path in the input field
        Assert.Contains("id=\"fileName\"", responseContent);
        Assert.Contains("tmp", responseContent); // Should contain temp directory path
        
        // Verify the file upload log entry is present
        Assert.Contains("File uploaded: test.csv", responseContent);
        Assert.Contains("B", responseContent); // Should show file size in bytes
        Assert.Contains("hx-swap-oob=\"afterbegin:#upload-log\"", responseContent);
    }

    [Fact] 
    public async Task PostFileSelected_JsonType_ReturnsOriginalFileName()
    {
        // Get the page and extract anti-forgery token
        var getResponse = await _client.GetAsync("/ClaimsDataImporter");
        getResponse.EnsureSuccessStatusCode();
        var getContent = await getResponse.Content.ReadAsStringAsync();
        
        var tokenStart = getContent.IndexOf("__RequestVerificationToken\" type=\"hidden\" value=\"") + "__RequestVerificationToken\" type=\"hidden\" value=\"".Length;
        var tokenEnd = getContent.IndexOf("\"", tokenStart);
        var token = getContent.Substring(tokenStart, tokenEnd - tokenStart);

        // Test JSON file type (should return original filename, not "zzz")
        var formData = new List<KeyValuePair<string, string>>
        {
            new("fileType", "json"),
            new("fileName", "config.json"),
            new("action", "ok"),
            new("__RequestVerificationToken", token)
        };

        var formContent = new FormUrlEncodedContent(formData);
        var response = await _client.PostAsync("/ClaimsDataImporter?handler=FileSelected", formContent);
        
        response.EnsureSuccessStatusCode();
        var responseContent = await response.Content.ReadAsStringAsync();
        
        // JSON type should return just the status message, not "zzz"
        Assert.Contains("user pressed ok", responseContent);
        Assert.DoesNotContain("zzz", responseContent);
    }
}