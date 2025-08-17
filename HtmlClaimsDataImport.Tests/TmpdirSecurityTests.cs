using System.IO;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;

namespace HtmlClaimsDataImport.Tests;

[Collection("WebApp")]
public class TmpdirSecurityTests(WebApplicationFactory<Program> factory)
{
    private readonly WebApplicationFactory<Program> _factory = factory;

    [Theory]
    [InlineData("/etc/passwd")]
    [InlineData("../../../etc/passwd")]
    [InlineData("/home/user/")]
    [InlineData("C:\\Windows\\System32")]
    [InlineData("/root/")]
    public async Task FileUpload_MaliciousTmpdir_RevertToSessionLogic(string maliciousTmpdir)
    {
        using var client = _factory.CreateClient();
        
        // Get the page and extract anti-forgery token
        var getResponse = await client.GetAsync("/ClaimsDataImporter");
        getResponse.EnsureSuccessStatusCode();
        var getContent = await getResponse.Content.ReadAsStringAsync();
        
        var tokenStart = getContent.IndexOf("__RequestVerificationToken\" type=\"hidden\" value=\"") + "__RequestVerificationToken\" type=\"hidden\" value=\"".Length;
        var tokenEnd = getContent.IndexOf("\"", tokenStart);
        var token = getContent[tokenStart..tokenEnd];
        
        // Create test file
        var testFileContent = "{\"test\": \"security\"}";
        var fileBytes = Encoding.UTF8.GetBytes(testFileContent);

        // Prepare form data with malicious tmpdir
        using var formData = new MultipartFormDataContent
        {
            { new StringContent("json"), "fileType" },
            { new StringContent(token), "__RequestVerificationToken" },
            { new StringContent(maliciousTmpdir), "tmpdir" },
            { new ByteArrayContent(fileBytes), "uploadedFile", "test.json" }
        };

        // Upload file with malicious tmpdir
        var response = await client.PostAsync("/ClaimsDataImporter?handler=FileUpload", formData);
        response.EnsureSuccessStatusCode();
        
        var responseContent = await response.Content.ReadAsStringAsync();
        
        // Verify file was uploaded successfully (should use session logic, not malicious path)
        Assert.Contains("test.json", responseContent);
        Assert.Contains("data-file-path", responseContent);
        
        // Extract the actual file path from response
        var filePathStart = responseContent.IndexOf("data-file-path=\"") + "data-file-path=\"".Length;
        var filePathEnd = responseContent.IndexOf("\"", filePathStart);
        var actualFilePath = responseContent[filePathStart..filePathEnd];
        
        // Verify the file path does NOT contain the malicious path
        Assert.DoesNotContain(maliciousTmpdir, actualFilePath);
        
        // Verify the file path contains "session-" (indicating session-based fallback was used)
        Assert.Contains("session-", actualFilePath);
        
        Console.WriteLine($"✅ SUCCESS: Malicious tmpdir '{maliciousTmpdir}' was rejected, file saved to safe location: {actualFilePath}");
    }
}