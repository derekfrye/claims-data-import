using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;

namespace HtmlClaimsDataImport.Tests;

[Collection("WebApp")]
public class FileSelectionIntegrationTests
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
        // Use simple session client approach like the working test
        using var sessionClient = _factory.CreateClient();
        
        // Get the page and token using session client
        var getResponse = await sessionClient.GetAsync("/ClaimsDataImporter");
        getResponse.EnsureSuccessStatusCode();
        var getContent = await getResponse.Content.ReadAsStringAsync();
        
        // Extract token
        var tokenStart = getContent.IndexOf("__RequestVerificationToken\" type=\"hidden\" value=\"") + "__RequestVerificationToken\" type=\"hidden\" value=\"".Length;
        var tokenEnd = getContent.IndexOf("\"", tokenStart);
        var token = getContent[tokenStart..tokenEnd];
        
        // Create a test CSV file content
        var testFileContent = "Name,Age,City\nJohn,25,New York\nJane,30,Boston";
        var fileBytes = Encoding.UTF8.GetBytes(testFileContent);

        // Prepare multipart form data for file upload
        using var formData = new MultipartFormDataContent
        {
            { new StringContent("filename"), "fileType" },
            { new StringContent(token), "__RequestVerificationToken" },
            { new ByteArrayContent(fileBytes), "uploadedFile", "test.csv" }
        };

        // Make the POST request to the file upload handler using session client
        var response = await sessionClient.PostAsync("/ClaimsDataImporter?handler=FileUpload", formData);
        
        // Ensure the request was successful
        response.EnsureSuccessStatusCode();
        
        // Get the response content
        var responseContent = await response.Content.ReadAsStringAsync();
        
        // Verify the response contains the expected HTML with temp file path in the input field
        Assert.Contains("id=\"fileName\"", responseContent);
        // Now we should see session-based temp directory instead of hardcoded path
        Assert.Contains("session-", responseContent); // Should contain session-based temp directory
        
        // Verify the file upload log entry is present
        Assert.Contains("File uploaded: test.csv", responseContent);
        Assert.Contains("B", responseContent); // Should show file size in bytes
        Assert.Contains("hx-swap-oob=\"afterbegin:#upload-log\"", responseContent);
    }

    [Fact] 
    public async Task PostFileSelected_JsonType_ReturnsOriginalFileName()
    {
        // Use session client for consistency
        using var sessionClient = _factory.CreateClient();
        
        // Get the page and extract anti-forgery token
        var getResponse = await sessionClient.GetAsync("/ClaimsDataImporter");
        getResponse.EnsureSuccessStatusCode();
        var getContent = await getResponse.Content.ReadAsStringAsync();
        
        var tokenStart = getContent.IndexOf("__RequestVerificationToken\" type=\"hidden\" value=\"") + "__RequestVerificationToken\" type=\"hidden\" value=\"".Length;
        var tokenEnd = getContent.IndexOf("\"", tokenStart);
        var token = getContent[tokenStart..tokenEnd];

        // Test JSON file type (should return original filename, not "zzz")
        var formData = new List<KeyValuePair<string, string>>
        {
            new("fileType", "json"),
            new("fileName", "config.json"),
            new("action", "ok"),
            new("__RequestVerificationToken", token)
        };

        var formContent = new FormUrlEncodedContent(formData);
        var response = await sessionClient.PostAsync("/ClaimsDataImporter?handler=FileSelected", formContent);
        
        response.EnsureSuccessStatusCode();
        var responseContent = await response.Content.ReadAsStringAsync();
        
        // JSON type should return just the status message, not "zzz"
        Assert.Contains("user pressed ok", responseContent);
        Assert.DoesNotContain("zzz", responseContent);
    }
}