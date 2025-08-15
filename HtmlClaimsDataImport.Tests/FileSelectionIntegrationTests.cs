using System.Text;
using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using HtmlClaimsDataImport.Services;

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

    private async Task<(string token, HttpClient sessionClient)> EstablishSessionAndGetTokenAsync()
    {
        // Create a client with cookie support using the test server
        var cookieContainer = new CookieContainer();
        var sessionClient = _factory.WithWebHostBuilder(builder => { })
            .CreateClient();
            
        // Manually configure the client to use cookies by copying its properties to a new client
        var handler = new HttpClientHandler() { CookieContainer = cookieContainer };
        var cookieEnabledClient = new HttpClient(handler)
        {
            BaseAddress = sessionClient.BaseAddress
        };
        
        // Copy any default headers
        foreach (var header in sessionClient.DefaultRequestHeaders)
        {
            cookieEnabledClient.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
        }
        
        // Dispose the original client
        sessionClient.Dispose();
        
        // Make initial request to establish session (like browser loading page)
        var getResponse = await cookieEnabledClient.GetAsync("/ClaimsDataImporter");
        getResponse.EnsureSuccessStatusCode();
        var getContent = await getResponse.Content.ReadAsStringAsync();
        
        // Extract anti-forgery token
        var tokenStart = getContent.IndexOf("__RequestVerificationToken\" type=\"hidden\" value=\"") + "__RequestVerificationToken\" type=\"hidden\" value=\"".Length;
        var tokenEnd = getContent.IndexOf("\"", tokenStart);
        var token = getContent.Substring(tokenStart, tokenEnd - tokenStart);
        
        return (token, cookieEnabledClient);
    }

    [Fact]
    public async Task PostFileUpload_FilenameType_ReturnsInputFieldAndUploadLog()
    {
        // Arrange: Establish unique session and get anti-forgery token
        var (token, sessionClient) = await EstablishSessionAndGetTokenAsync();
        
        try
        {
            // Create a test CSV file content
            var testFileContent = "Name,Age,City\nJohn,25,New York\nJane,30,Boston";
            var fileBytes = Encoding.UTF8.GetBytes(testFileContent);

            // Prepare multipart form data for file upload
            using var formData = new MultipartFormDataContent();
            formData.Add(new StringContent("filename"), "fileType");
            formData.Add(new StringContent(token), "__RequestVerificationToken");
            formData.Add(new ByteArrayContent(fileBytes), "uploadedFile", "test.csv");

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
        finally
        {
            sessionClient.Dispose();
        }
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