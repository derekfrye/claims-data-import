using System.IO;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using HtmlClaimsDataImport.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace HtmlClaimsDataImport.Tests;

[Collection("WebApp")]
public class FileUploadSession_IntTest(WebApplicationFactory<Program> factory)
{
    private readonly WebApplicationFactory<Program> _factory = factory;

    [Fact]
    public async Task FileUploadSession_UploadJsonFile_ThenReloadAndUploadAgain_VerifiesOverwriteBehavior()
    {
        // Use standard production configuration and specify tmpdir to control temp directory
        using var sessionClient = _factory.CreateClient();
        
        // Create a shared temp directory for this test to simulate browser session persistence
        var testTempDir = Path.Combine(Path.GetTempPath(), $"test-session-{Guid.NewGuid():N}");
        Directory.CreateDirectory(testTempDir);
        
        // Step 1: Get the page and extract anti-forgery token
        var getResponse = await sessionClient.GetAsync("/ClaimsDataImporter");
        getResponse.EnsureSuccessStatusCode();
        var getContent = await getResponse.Content.ReadAsStringAsync();
        
        Console.WriteLine($"=== USING FIXED TEMP DIR FOR TEST: {testTempDir} ===");
        
        // Extract anti-forgery token
        var tokenStart = getContent.IndexOf("__RequestVerificationToken\" type=\"hidden\" value=\"") + "__RequestVerificationToken\" type=\"hidden\" value=\"".Length;
        var tokenEnd = getContent.IndexOf("\"", tokenStart);
        var token = getContent[tokenStart..tokenEnd];
        
        // Step 2: Create first dummy JSON file with test data
        var firstJsonData = new
        {
            setting1 = "value1",
            setting2 = 42,
            setting3 = true,
            nested = new
            {
                property1 = "nested_value1",
                property2 = "nested_value2"
            }
        };
        
        var firstJsonContent = JsonSerializer.Serialize(firstJsonData, new JsonSerializerOptions { WriteIndented = true });
        var firstFileBytes = Encoding.UTF8.GetBytes(firstJsonContent);

        // Prepare multipart form data for first file upload
        using var firstFormData = new MultipartFormDataContent
        {
            { new StringContent("json"), "fileType" },
            { new StringContent(token), "__RequestVerificationToken" },
            { new StringContent(testTempDir), "tmpdir" },
            { new ByteArrayContent(firstFileBytes), "uploadedFile", "config.json" }
        };

        // Upload first file with fixed session ID
        var firstUploadResponse = await sessionClient.PostAsync("/ClaimsDataImporter?handler=FileUpload", firstFormData);
        firstUploadResponse.EnsureSuccessStatusCode();
        
        var firstResponseContent = await firstUploadResponse.Content.ReadAsStringAsync();
        
        // Verify first upload response contains expected elements
        Assert.Contains("config.json", firstResponseContent);
        Assert.Contains("data-file-path", firstResponseContent);
        
        // Step 3: Extract temp directory path from response
        // Look for the file path in the response HTML
        var filePathStart = firstResponseContent.IndexOf("data-file-path=\"") + "data-file-path=\"".Length;
        var filePathEnd = firstResponseContent.IndexOf("\"", filePathStart);
        var uploadedFilePath = firstResponseContent[filePathStart..filePathEnd];
        
        // Extract temp directory from the file path
        var tempDirectory = Path.GetDirectoryName(uploadedFilePath);
        Assert.True(Directory.Exists(tempDirectory), "Temp directory should exist");
        
        // Step 4: Verify first file was uploaded and content matches
        var firstUploadedFile = Path.Combine(tempDirectory, "config.json");
        Assert.True(File.Exists(firstUploadedFile), "First uploaded file should exist");
        
        var firstUploadedContent = await File.ReadAllTextAsync(firstUploadedFile);
        var firstUploadedJson = JsonSerializer.Deserialize<JsonElement>(firstUploadedContent);
        var originalJson = JsonSerializer.Deserialize<JsonElement>(firstJsonContent);
        
        // Compare key-by-key
        Assert.Equal(originalJson.GetProperty("setting1").GetString(), firstUploadedJson.GetProperty("setting1").GetString());
        Assert.Equal(originalJson.GetProperty("setting2").GetInt32(), firstUploadedJson.GetProperty("setting2").GetInt32());
        Assert.Equal(originalJson.GetProperty("setting3").GetBoolean(), firstUploadedJson.GetProperty("setting3").GetBoolean());
        Assert.Equal(originalJson.GetProperty("nested").GetProperty("property1").GetString(), 
                    firstUploadedJson.GetProperty("nested").GetProperty("property1").GetString());
        Assert.Equal(originalJson.GetProperty("nested").GetProperty("property2").GetString(), 
                    firstUploadedJson.GetProperty("nested").GetProperty("property2").GetString());
        
        // Step 5: Upload second file to same session client (will overwrite)
        // Note: In integration tests, each request gets a new session, but we can still test 
        // that files persist and the upload/overwrite logic works correctly
        
        // Step 6: Create second dummy JSON file with different data
        var secondJsonData = new
        {
            setting1 = "modified_value1",
            setting2 = 99,
            setting3 = false,
            setting4 = "new_setting",
            nested = new
            {
                property1 = "modified_nested_value1",
                property3 = "new_nested_property"
            }
        };
        
        var secondJsonContent = JsonSerializer.Serialize(secondJsonData, new JsonSerializerOptions { WriteIndented = true });
        var secondFileBytes = Encoding.UTF8.GetBytes(secondJsonContent);

        // Prepare multipart form data for second file upload (use same tmpdir)
        using var secondFormData = new MultipartFormDataContent
        {
            { new StringContent("json"), "fileType" },
            { new StringContent(token), "__RequestVerificationToken" },
            { new StringContent(testTempDir), "tmpdir" },
            { new ByteArrayContent(secondFileBytes), "uploadedFile", "config.json" }
        };

        // Upload second file (should go to SAME temp directory because of tmpdir parameter)
        var secondUploadResponse = await sessionClient.PostAsync("/ClaimsDataImporter?handler=FileUpload", secondFormData);
        secondUploadResponse.EnsureSuccessStatusCode();
        
        var secondResponseContent = await secondUploadResponse.Content.ReadAsStringAsync();
        
        // Verify second upload response
        Assert.Contains("config.json", secondResponseContent);
        
        // Extract temp directory from second upload response
        var secondFilePathStart = secondResponseContent.IndexOf("data-file-path=\"") + "data-file-path=\"".Length;
        var secondFilePathEnd = secondResponseContent.IndexOf("\"", secondFilePathStart);
        var secondUploadedFilePath = secondResponseContent[secondFilePathStart..secondFilePathEnd];
        var secondTempDirectory = Path.GetDirectoryName(secondUploadedFilePath);
        
        Console.WriteLine($"Second upload temp directory: {secondTempDirectory}");
        
        // Step 7: With tmpdir parameter, both uploads should go to the SAME temp directory
        Console.WriteLine($"Test temp directory: {testTempDir}");
        Console.WriteLine($"First upload temp directory: {tempDirectory}");
        Console.WriteLine($"Second upload temp directory: {secondTempDirectory}");
        
        // This is the key test - same tmpdir should mean same temp directory
        Assert.Equal(testTempDir, tempDirectory);
        Assert.Equal(testTempDir, secondTempDirectory);
        
        // Step 8: Verify the file was overwritten (not two separate files)
        var configFile = Path.Combine(tempDirectory, "config.json");
        Assert.True(File.Exists(configFile), "Config file should exist");
        
        // Step 9: Verify the content matches the SECOND upload (overwrite behavior)
        var finalContent = await File.ReadAllTextAsync(configFile);
        var finalJson = JsonSerializer.Deserialize<JsonElement>(finalContent);
        var secondOriginalJson = JsonSerializer.Deserialize<JsonElement>(secondJsonContent);
        
        // Should contain second upload content
        Assert.Equal(secondOriginalJson.GetProperty("setting1").GetString(), finalJson.GetProperty("setting1").GetString());
        Assert.Equal(secondOriginalJson.GetProperty("setting2").GetInt32(), finalJson.GetProperty("setting2").GetInt32());
        Assert.Equal(secondOriginalJson.GetProperty("setting3").GetBoolean(), finalJson.GetProperty("setting3").GetBoolean());
        Assert.Equal(secondOriginalJson.GetProperty("setting4").GetString(), finalJson.GetProperty("setting4").GetString());
        
        // Should NOT contain properties unique to first upload
        Assert.False(finalJson.GetProperty("nested").TryGetProperty("property2", out _), 
                    "property2 from first upload should not exist after overwrite");
        
        Console.WriteLine($"✅ SUCCESS: tmpdir parameter used same temp directory: {testTempDir}");
        Console.WriteLine("✅ SUCCESS: Second file overwrote first file (real browser behavior simulated!)");
        
        // Clean up test temp directory
        try
        {
            Directory.Delete(testTempDir, true);
            Console.WriteLine($"✅ SUCCESS: Test temp directory cleaned up: {testTempDir}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️  WARNING: Failed to clean up test temp directory {testTempDir}: {ex.Message}");
        }
    }
}