using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace HtmlClaimsDataImport.Tests;

[Collection("WebApp")]
public class FileUploadPersistence_IntTest(WebApplicationFactory<Program> factory)
{
    private readonly WebApplicationFactory<Program> _factory = factory;

    [Fact]
    public async Task FileUpload_CreatesFileInTempDirectory_FilePerisistsAfterRequest()
    {
        using var sessionClient = _factory.CreateClient();
        
        // Step 1: Get the page and extract anti-forgery token
        var getResponse = await sessionClient.GetAsync("/ClaimsDataImporter");
        getResponse.EnsureSuccessStatusCode();
        var getContent = await getResponse.Content.ReadAsStringAsync();
        
        // Extract anti-forgery token
        var tokenStart = getContent.IndexOf("__RequestVerificationToken\" type=\"hidden\" value=\"") + "__RequestVerificationToken\" type=\"hidden\" value=\"".Length;
        var tokenEnd = getContent.IndexOf("\"", tokenStart);
        var token = getContent[tokenStart..tokenEnd];
        
        // Step 2: Create dummy JSON file with test data
        var jsonData = new
        {
            testSetting = "testValue",
            anotherSetting = 42,
            booleanSetting = true
        };
        
        var jsonContent = JsonSerializer.Serialize(jsonData, new JsonSerializerOptions { WriteIndented = true });
        var fileBytes = Encoding.UTF8.GetBytes(jsonContent);

        // Prepare multipart form data for file upload
        using var formData = new MultipartFormDataContent
        {
            { new StringContent("json"), "fileType" },
            { new StringContent(token), "__RequestVerificationToken" },
            { new ByteArrayContent(fileBytes), "uploadedFile", "config.json" }
        };

        // Step 3: Upload file
        var uploadResponse = await sessionClient.PostAsync("/ClaimsDataImporter?handler=FileUpload", formData);
        uploadResponse.EnsureSuccessStatusCode();
        
        var responseContent = await uploadResponse.Content.ReadAsStringAsync();
        
        // Verify upload response contains expected elements
        Assert.Contains("config.json", responseContent);
        Assert.Contains("data-file-path", responseContent);
        
        // Step 4: Extract temp directory path from response
        var filePathStart = responseContent.IndexOf("data-file-path=\"") + "data-file-path=\"".Length;
        var filePathEnd = responseContent.IndexOf("\"", filePathStart);
        var uploadedFilePath = responseContent[filePathStart..filePathEnd];
        
        // Extract temp directory from the file path
        var tempDirectory = Path.GetDirectoryName(uploadedFilePath);
        var uploadedFile = Path.Combine(tempDirectory!, "config.json");
        
        // Step 5: Verify file exists and content matches (this should pass with our fix)
        Assert.True(Directory.Exists(tempDirectory), "Temp directory should exist after upload");
        Assert.True(File.Exists(uploadedFile), "Uploaded file should exist after request completes");
        
        var uploadedContent = await File.ReadAllTextAsync(uploadedFile);
        var uploadedJson = JsonSerializer.Deserialize<JsonElement>(uploadedContent);
        var originalJson = JsonSerializer.Deserialize<JsonElement>(jsonContent);
        
        // Compare key-by-key
        Assert.Equal(originalJson.GetProperty("testSetting").GetString(), uploadedJson.GetProperty("testSetting").GetString());
        Assert.Equal(originalJson.GetProperty("anotherSetting").GetInt32(), uploadedJson.GetProperty("anotherSetting").GetInt32());
        Assert.Equal(originalJson.GetProperty("booleanSetting").GetBoolean(), uploadedJson.GetProperty("booleanSetting").GetBoolean());
        
        // Step 6: Make another request to the same endpoint to simulate page interaction
        var secondGetResponse = await sessionClient.GetAsync("/ClaimsDataImporter");
        secondGetResponse.EnsureSuccessStatusCode();
        
        // Step 7: Verify file STILL exists after second request (this is the key bug fix test)
        Assert.True(Directory.Exists(tempDirectory), "Temp directory should STILL exist after second request");
        Assert.True(File.Exists(uploadedFile), "Uploaded file should STILL exist after second request");
        
        // Step 8: Verify content is still intact
        var finalContent = await File.ReadAllTextAsync(uploadedFile);
        var finalJson = JsonSerializer.Deserialize<JsonElement>(finalContent);
        
        Assert.Equal(originalJson.GetProperty("testSetting").GetString(), finalJson.GetProperty("testSetting").GetString());
        Assert.Equal(originalJson.GetProperty("anotherSetting").GetInt32(), finalJson.GetProperty("anotherSetting").GetInt32());
        Assert.Equal(originalJson.GetProperty("booleanSetting").GetBoolean(), finalJson.GetProperty("booleanSetting").GetBoolean());
        
        Console.WriteLine($"✅ SUCCESS: File persisted through multiple requests in temp directory: {tempDirectory}");
        Console.WriteLine($"✅ SUCCESS: File content verified intact after multiple HTTP requests");
    }
}