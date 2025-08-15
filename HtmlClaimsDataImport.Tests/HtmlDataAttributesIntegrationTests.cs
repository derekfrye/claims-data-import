using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using AngleSharp;
using AngleSharp.Html.Dom;
using System.Text;
using HtmlClaimsDataImport.Services;

namespace HtmlClaimsDataImport.Tests;

public class HtmlDataAttributesIntegrationTests : IClassFixture<HtmlDataAttributesIntegrationTests.CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public HtmlDataAttributesIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly string _testTempDir;
        private readonly string _testSessionId;

        public CustomWebApplicationFactory()
        {
            // Create unique temp directory and session ID for this test run
            var baseDir = Path.GetDirectoryName(typeof(CustomWebApplicationFactory).Assembly.Location);
            _testSessionId = Path.GetRandomFileName();
            _testTempDir = Path.Combine(baseDir!, "test-temp", _testSessionId);
            Directory.CreateDirectory(_testTempDir);
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Replace the ITempDirectoryService with our test implementation
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ITempDirectoryService));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }
                services.AddScoped<ITempDirectoryService>(provider =>
                {
                    return new TempDirectoryService(_testSessionId, _testTempDir);
                });
            });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    if (Directory.Exists(_testTempDir))
                    {
                        Directory.Delete(_testTempDir, true);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to cleanup test temp directory {_testTempDir}: {ex.Message}");
                }
            }
            base.Dispose(disposing);
        }
    }

    private async Task<string> GetAntiForgeryTokenAsync()
    {
        var getResponse = await _client.GetAsync("/ClaimsDataImporter");
        getResponse.EnsureSuccessStatusCode();
        var getContent = await getResponse.Content.ReadAsStringAsync();
        
        var tokenStart = getContent.IndexOf("__RequestVerificationToken\" type=\"hidden\" value=\"") + "__RequestVerificationToken\" type=\"hidden\" value=\"".Length;
        var tokenEnd = getContent.IndexOf("\"", tokenStart);
        return getContent.Substring(tokenStart, tokenEnd - tokenStart);
    }

    [Fact]
    public async Task FileUpload_JsonFile_ReturnsCorrectDataAttributes()
    {
        // Arrange: Get anti-forgery token first
        var getResponse = await _client.GetAsync("/ClaimsDataImporter");
        getResponse.EnsureSuccessStatusCode();
        var getContent = await getResponse.Content.ReadAsStringAsync();
        
        var tokenStart = getContent.IndexOf("__RequestVerificationToken\" type=\"hidden\" value=\"") + "__RequestVerificationToken\" type=\"hidden\" value=\"".Length;
        var tokenEnd = getContent.IndexOf("\"", tokenStart);
        var token = getContent.Substring(tokenStart, tokenEnd - tokenStart);
        
        // Create a test JSON file
        var jsonContent = """{"testKey": "testValue"}""";
        var jsonBytes = Encoding.UTF8.GetBytes(jsonContent);
        
        using var content = new MultipartFormDataContent
        {
            { new StringContent("json"), "fileType" },
            { new StringContent(token), "__RequestVerificationToken" },
            { new ByteArrayContent(jsonBytes), "uploadedFile", "test-config.json" }
        };
        
        // Act: Post file upload request (simulating HTMX request)
        var response = await _client.PostAsync("/ClaimsDataImporter?handler=FileUpload", content);
        
        // Assert: Response should be successful
        response.EnsureSuccessStatusCode();
        var responseHtml = await response.Content.ReadAsStringAsync();
        
        // Parse HTML using AngleSharp
        var config = Configuration.Default;
        var context = BrowsingContext.New(config);
        var document = await context.OpenAsync(req => req.Content(responseHtml));
        
        // Verify status span has correct data attribute
        var statusSpan = document.QuerySelector("#json-status") as IHtmlSpanElement;
        Assert.NotNull(statusSpan);
        Assert.Contains("File uploaded: test-config.json", statusSpan.TextContent);
        Assert.Equal(statusSpan.TextContent, statusSpan.GetAttribute("data-status"));
        
        // Verify input has correct data attribute  
        var inputElement = document.QuerySelector("#jsonFile") as IHtmlInputElement;
        Assert.NotNull(inputElement);
        Assert.Contains("test-config.json", inputElement.Value);
        Assert.Equal(inputElement.Value, inputElement.GetAttribute("data-file-path"));
        
        // Verify log div has data attribute
        var logDiv = document.QuerySelector("div[data-log-entry]") as IHtmlDivElement;
        Assert.NotNull(logDiv);
        Assert.NotNull(logDiv.GetAttribute("data-log-entry"));
        Assert.Contains("test-config.json", logDiv.GetAttribute("data-log-entry"));
    }

    [Fact]
    public async Task FileUpload_CsvFile_ReturnsCorrectDataAttributes()
    {
        // Arrange: Get anti-forgery token
        var token = await GetAntiForgeryTokenAsync();
        
        // Create a test CSV file
        var csvContent = "Name,Age,City\nJohn,30,NYC\nJane,25,LA";
        var csvBytes = Encoding.UTF8.GetBytes(csvContent);
        
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent("filename"), "fileType");
        content.Add(new StringContent(token), "__RequestVerificationToken");
        content.Add(new ByteArrayContent(csvBytes), "uploadedFile", "test-data.csv");
        
        // Act: Post file upload request
        var response = await _client.PostAsync("/ClaimsDataImporter?handler=FileUpload", content);
        
        // Assert: Response should be successful
        response.EnsureSuccessStatusCode();
        var responseHtml = await response.Content.ReadAsStringAsync();
        
        // Parse HTML using AngleSharp
        var config = Configuration.Default;
        var context = BrowsingContext.New(config);
        var document = await context.OpenAsync(req => req.Content(responseHtml));
        
        // Verify status span has correct data attribute
        var statusSpan = document.QuerySelector("#filename-status") as IHtmlSpanElement;
        Assert.NotNull(statusSpan);
        Assert.Contains("File uploaded: test-data.csv", statusSpan.TextContent);
        Assert.Equal(statusSpan.TextContent, statusSpan.GetAttribute("data-status"));
        
        // Verify input has correct data attribute
        var inputElement = document.QuerySelector("#fileName") as IHtmlInputElement;
        Assert.NotNull(inputElement);
        Assert.Contains("test-data.csv", inputElement.Value);
        Assert.Equal(inputElement.Value, inputElement.GetAttribute("data-file-path"));
    }

    [Fact]
    public async Task FileUpload_DatabaseFile_ReturnsCorrectDataAttributes()
    {
        // Arrange: Get anti-forgery token
        var token = await GetAntiForgeryTokenAsync();
        
        // Create a test database file (empty SQLite file)
        var dbBytes = new byte[] { 0x53, 0x51, 0x4C, 0x69, 0x74, 0x65, 0x20, 0x66 }; // "SQLite f" header
        
        using var content = new MultipartFormDataContent
        {
            { new StringContent("database"), "fileType" },
            { new StringContent(token), "__RequestVerificationToken" },
            { new ByteArrayContent(dbBytes), "uploadedFile", "test-database.db" }
        };
        
        // Act: Post file upload request
        var response = await _client.PostAsync("/ClaimsDataImporter?handler=FileUpload", content);
        
        // Assert: Response should be successful
        response.EnsureSuccessStatusCode();
        var responseHtml = await response.Content.ReadAsStringAsync();
        
        // Parse HTML using AngleSharp
        var config = Configuration.Default;
        var context = BrowsingContext.New(config);
        var document = await context.OpenAsync(req => req.Content(responseHtml));
        
        // Verify status span has correct data attribute
        var statusSpan = document.QuerySelector("#database-status") as IHtmlSpanElement;
        Assert.NotNull(statusSpan);
        Assert.Contains("File uploaded: test-database.db", statusSpan.TextContent);
        Assert.Equal(statusSpan.TextContent, statusSpan.GetAttribute("data-status"));
        
        // Verify input has correct data attribute
        var inputElement = document.QuerySelector("#database") as IHtmlInputElement;
        Assert.NotNull(inputElement);
        Assert.Contains("test-database.db", inputElement.Value);
        Assert.Equal(inputElement.Value, inputElement.GetAttribute("data-file-path"));
    }

    [Fact]
    public async Task HtmxResponse_ContainsOutOfBandUpdates()
    {
        // Arrange: Get anti-forgery token
        var token = await GetAntiForgeryTokenAsync();
        
        // Create test file
        var jsonContent = """{"test": "value"}""";
        var jsonBytes = Encoding.UTF8.GetBytes(jsonContent);
        
        using var content = new MultipartFormDataContent
        {
            { new StringContent("json"), "fileType" },
            { new StringContent(token), "__RequestVerificationToken" },
            { new ByteArrayContent(jsonBytes), "uploadedFile", "config.json" }
        };
        
        // Act: Post file upload request
        var response = await _client.PostAsync("/ClaimsDataImporter?handler=FileUpload", content);
        
        // Assert: Response should contain HTMX out-of-band updates
        response.EnsureSuccessStatusCode();
        var responseHtml = await response.Content.ReadAsStringAsync();
        
        // Verify HTMX hx-swap-oob attributes are present
        Assert.Contains("hx-swap-oob=\"outerHTML\"", responseHtml);
        Assert.Contains("hx-swap-oob=\"afterbegin:#upload-log\"", responseHtml);
        
        // Parse and verify structure
        var config = Configuration.Default;
        var context = BrowsingContext.New(config);
        var document = await context.OpenAsync(req => req.Content(responseHtml));
        
        // Verify input has hx-swap-oob attribute
        var inputElement = document.QuerySelector("input[hx-swap-oob]");
        Assert.NotNull(inputElement);
        Assert.Equal("outerHTML", inputElement.GetAttribute("hx-swap-oob"));
        
        // Verify log div has hx-swap-oob attribute  
        var logDiv = document.QuerySelector("div[hx-swap-oob]");
        Assert.NotNull(logDiv);
        Assert.Equal("afterbegin:#upload-log", logDiv.GetAttribute("hx-swap-oob"));
    }

    [Fact]
    public async Task FileUpload_NoFile_ReturnsErrorMessage()
    {
        // Arrange: Get anti-forgery token
        var token = await GetAntiForgeryTokenAsync();
        
        // Create request with no file
        using var content = new MultipartFormDataContent
        {
            { new StringContent("json"), "fileType" },
            { new StringContent(token), "__RequestVerificationToken" }
        };
        // No uploadedFile added intentionally
        
        // Act: Post file upload request
        var response = await _client.PostAsync("/ClaimsDataImporter?handler=FileUpload", content);
        
        // Assert: Should return error message
        var responseContent = await response.Content.ReadAsStringAsync();
        Assert.Contains("No file selected", responseContent);
    }

    [Theory]
    [InlineData("json", "#json-status", "#jsonFile")]
    [InlineData("filename", "#filename-status", "#fileName")]  
    [InlineData("database", "#database-status", "#database")]
    public async Task FileUpload_AllFileTypes_HaveConsistentDataAttributeStructure(
        string fileType, 
        string expectedStatusId, 
        string expectedInputId)
    {
        // Arrange: Create test file
        var fileContent = fileType switch
        {
            "json" => """{"key": "value"}""",
            "filename" => "header1,header2\nvalue1,value2",
            "database" => "SQLite format 3\0",
            _ => "test content"
        };
        var extension = fileType switch
        {
            "json" => ".json",
            "filename" => ".csv",
            "database" => ".db",
            _ => ".txt"
        };
        
        var fileBytes = Encoding.UTF8.GetBytes(fileContent);
        
        // Get anti-forgery token
        var token = await GetAntiForgeryTokenAsync();
        
        using var content = new MultipartFormDataContent
        {
            { new StringContent(fileType), "fileType" },
            { new StringContent(token), "__RequestVerificationToken" },
            { new ByteArrayContent(fileBytes), "uploadedFile", $"test{extension}" }
        };
        
        // Act: Post file upload request
        var response = await _client.PostAsync("/ClaimsDataImporter?handler=FileUpload", content);
        
        // Assert: All file types should have consistent data attribute structure
        response.EnsureSuccessStatusCode();
        var responseHtml = await response.Content.ReadAsStringAsync();
        
        var config = Configuration.Default;
        var context = BrowsingContext.New(config);
        var document = await context.OpenAsync(req => req.Content(responseHtml));
        
        // Every response should have:
        // 1. Status span with data-status attribute
        var statusSpan = document.QuerySelector($"span[data-status]");
        Assert.NotNull(statusSpan);
        Assert.Equal(expectedStatusId.TrimStart('#'), statusSpan.Id);
        
        // 2. Input with data-file-path attribute  
        var inputElement = document.QuerySelector($"input[data-file-path]");
        Assert.NotNull(inputElement);
        Assert.Equal(expectedInputId.TrimStart('#'), inputElement.Id);
        
        // 3. Log div with data-log-entry attribute
        var logDiv = document.QuerySelector("div[data-log-entry]");
        Assert.NotNull(logDiv);
        
        // 4. Data attributes should match visible content
        Assert.Equal(statusSpan.TextContent, statusSpan.GetAttribute("data-status"));
        Assert.Equal(inputElement.GetAttribute("value"), inputElement.GetAttribute("data-file-path"));
    }
}