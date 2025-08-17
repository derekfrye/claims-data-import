using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using AngleSharp;
using AngleSharp.Html.Dom;

namespace HtmlClaimsDataImport.Tests;

[Collection("WebApp")]
public class PreviewPaneIntegrationTests
{
    private readonly WebApplicationFactory<Program> _factory;

    public PreviewPaneIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private async Task<string> GetAntiForgeryTokenAsync(HttpClient client)
    {
        var getResponse = await client.GetAsync("/ClaimsDataImporter");
        getResponse.EnsureSuccessStatusCode();
        var getContent = await getResponse.Content.ReadAsStringAsync();
        
        var tokenStart = getContent.IndexOf("__RequestVerificationToken\" type=\"hidden\" value=\"") + "__RequestVerificationToken\" type=\"hidden\" value=\"".Length;
        var tokenEnd = getContent.IndexOf("\"", tokenStart);
        return getContent[tokenStart..tokenEnd];
    }

    private async Task<(string responseHtml, string tmpdir)> UploadTestFileAsync(HttpClient client, string fileType, string fileName, string content, string? tmpdir = null)
    {
        var token = await GetAntiForgeryTokenAsync(client);
        var fileBytes = Encoding.UTF8.GetBytes(content);

        // Use provided tmpdir or create a test temp directory
        var actualTmpdir = tmpdir ?? Path.Combine(Path.GetTempPath(), $"test-session-{Guid.NewGuid():N}");
        if (!Directory.Exists(actualTmpdir))
        {
            Directory.CreateDirectory(actualTmpdir);
        }

        using var formData = new MultipartFormDataContent
        {
            { new StringContent(fileType), "fileType" },
            { new StringContent(token), "__RequestVerificationToken" },
            { new StringContent(actualTmpdir), "tmpdir" },
            { new ByteArrayContent(fileBytes), "uploadedFile", fileName }
        };

        var response = await client.PostAsync("/ClaimsDataImporter?handler=FileUpload", formData);
        response.EnsureSuccessStatusCode();
        var responseHtml = await response.Content.ReadAsStringAsync();
        
        return (responseHtml, actualTmpdir);
    }

    private async Task<string> LoadDataAsync(HttpClient client, string tmpdir, string csvFileName, string jsonPath = "default", string databasePath = "default")
    {
        var token = await GetAntiForgeryTokenAsync(client);

        var formData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("tmpdir", tmpdir),
            new KeyValuePair<string, string>("fileName", csvFileName),
            new KeyValuePair<string, string>("jsonPath", jsonPath),
            new KeyValuePair<string, string>("databasePath", databasePath),
            new KeyValuePair<string, string>("__RequestVerificationToken", token)
        });

        var response = await client.PostAsync("/ClaimsDataImporter?handler=LoadData", formData);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    private async Task<string> GetPreviewAsync(HttpClient client, int mappingStep = 0, string selectedColumn = "")
    {
        var token = await GetAntiForgeryTokenAsync(client);

        var formData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("tmpdir", ""), // Will use session temp directory
            new KeyValuePair<string, string>("mappingStep", mappingStep.ToString()),
            new KeyValuePair<string, string>("selectedColumn", selectedColumn),
            new KeyValuePair<string, string>("__RequestVerificationToken", token)
        });

        var response = await client.PostAsync("/ClaimsDataImporter?handler=Preview", formData);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    [Fact]
    public async Task PreviewPane_LoadData_ShowsTableWithData()
    {
        using var sessionClient = _factory.CreateClient();

        // Step 1: Upload test CSV file
        var csvContent = "ClaimID,Amount,Date,Description\n1,100.50,2024-01-15,Medical claim\n2,250.75,2024-01-16,Dental claim\n3,50.00,2024-01-17,Vision claim";
        await UploadTestFileAsync(sessionClient, "filename", "test-claims.csv", csvContent);

        // Step 2: Load data into database
        var loadResult = await LoadDataAsync(sessionClient, "test-claims.csv");
        Assert.Contains("file imported to table", loadResult);

        // Step 3: Get preview data
        var previewHtml = await GetPreviewAsync(sessionClient);

        // Step 4: Parse and verify preview content
        var config = Configuration.Default;
        var context = BrowsingContext.New(config);
        var document = await context.OpenAsync(req => req.Content(previewHtml));

        // Verify preview table exists
        var previewTable = document.QuerySelector("#previewTable") as IHtmlTableElement;
        Assert.NotNull(previewTable);

        // Verify table headers (should match CSV columns)
        var headers = document.QuerySelectorAll("th").Cast<IHtmlTableHeaderCellElement>().ToList();
        Assert.Contains(headers, h => h.TextContent.Contains("ClaimID"));
        Assert.Contains(headers, h => h.TextContent.Contains("Amount"));
        Assert.Contains(headers, h => h.TextContent.Contains("Date"));
        Assert.Contains(headers, h => h.TextContent.Contains("Description"));

        // Verify table contains data rows
        var dataRows = document.QuerySelectorAll("tbody tr").Cast<IHtmlTableRowElement>().ToList();
        Assert.True(dataRows.Count > 0, "Preview table should contain data rows");

        // Verify first row contains expected data
        var firstRowCells = dataRows[0].QuerySelectorAll("td").Cast<IHtmlTableDataCellElement>().ToList();
        Assert.Contains(firstRowCells, cell => cell.TextContent.Contains("1")); // ClaimID
        Assert.Contains(firstRowCells, cell => cell.TextContent.Contains("100.50")); // Amount
    }

    [Fact]
    public async Task PreviewPane_ClickColumn_HighlightsEntireColumn()
    {
        using var sessionClient = _factory.CreateClient();

        // Step 1: Setup test data
        var csvContent = "ClaimID,Amount,Date,Description\n1,100.50,2024-01-15,Medical claim\n2,250.75,2024-01-16,Dental claim";
        await UploadTestFileAsync(sessionClient, "filename", "test-claims.csv", csvContent);
        await LoadDataAsync(sessionClient, "test-claims.csv");

        // Step 2: Get initial preview (no column selected)
        var initialPreviewHtml = await GetPreviewAsync(sessionClient);
        var config = Configuration.Default;
        var context = BrowsingContext.New(config);
        var initialDocument = await context.OpenAsync(req => req.Content(initialPreviewHtml));

        // Verify no columns are initially highlighted
        var initialSelectedHeaders = initialDocument.QuerySelectorAll("th.selected-column-header");
        var initialSelectedCells = initialDocument.QuerySelectorAll("td.selected-column-cell");
        Assert.Empty(initialSelectedHeaders);
        Assert.Empty(initialSelectedCells);

        // Step 3: Simulate clicking the "Amount" column
        var previewWithSelectionHtml = await GetPreviewAsync(sessionClient, 0, "Amount");
        var selectedDocument = await context.OpenAsync(req => req.Content(previewWithSelectionHtml));

        // Step 4: Verify column highlighting
        var selectedHeaders = selectedDocument.QuerySelectorAll("th.selected-column-header").Cast<IHtmlTableHeaderCellElement>().ToList();
        var selectedCells = selectedDocument.QuerySelectorAll("td.selected-column-cell").Cast<IHtmlTableDataCellElement>().ToList();

        // Should have exactly one highlighted header (the Amount column)
        Assert.Single(selectedHeaders);
        Assert.Equal("Amount", selectedHeaders[0].TextContent.Trim());

        // Should have highlighted cells for each data row in the Amount column
        Assert.True(selectedCells.Count > 0, "Should have highlighted cells in selected column");
        
        // Verify the highlighted cells contain the expected Amount values
        Assert.Contains(selectedCells, cell => cell.TextContent.Contains("100.50"));
        Assert.Contains(selectedCells, cell => cell.TextContent.Contains("250.75"));
    }

    [Fact]
    public async Task PreviewPane_ColumnSelection_UpdatesMappingStep()
    {
        using var sessionClient = _factory.CreateClient();

        // Setup test data
        var csvContent = "ID,Price,Created,Notes\n1,99.99,2024-01-01,Test note";
        await UploadTestFileAsync(sessionClient, "filename", "test-data.csv", csvContent);
        await LoadDataAsync(sessionClient, "test-data.csv");

        // Test different mapping steps
        for (int step = 0; step < 3; step++)
        {
            var previewHtml = await GetPreviewAsync(sessionClient, step, "Price");
            var config = Configuration.Default;
            var context = BrowsingContext.New(config);
            var document = await context.OpenAsync(req => req.Content(previewHtml));

            // Verify mapping step is displayed correctly
            var mappingProgress = document.QuerySelector(".mapping-progress");
            Assert.NotNull(mappingProgress);
            Assert.Contains($"Step {step + 1}", mappingProgress.TextContent);

            // Verify selected column is highlighted regardless of mapping step
            var selectedHeaders = document.QuerySelectorAll("th.selected-column-header");
            Assert.Single(selectedHeaders);
        }
    }

    [Fact]
    public async Task PreviewPane_CornflowerBlueHighlighting_CorrectCssApplied()
    {
        using var sessionClient = _factory.CreateClient();

        // Setup and load test data
        var csvContent = "Name,Value,Status\nTest1,123,Active\nTest2,456,Inactive";
        await UploadTestFileAsync(sessionClient, "filename", "test.csv", csvContent);
        await LoadDataAsync(sessionClient, "test.csv");

        // Get preview with "Value" column selected
        var previewHtml = await GetPreviewAsync(sessionClient, 0, "Value");
        
        // Verify CSS contains the cornflower blue color definition
        Assert.Contains("#6495ED", previewHtml); // Hex code for cornflower blue
        Assert.Contains("selected-column-header", previewHtml);
        Assert.Contains("selected-column-cell", previewHtml);
        Assert.Contains("background-color: #6495ED !important", previewHtml);
        Assert.Contains("color: white", previewHtml);

        // Parse and verify DOM structure
        var config = Configuration.Default;
        var context = BrowsingContext.New(config);
        var document = await context.OpenAsync(req => req.Content(previewHtml));

        // Verify the selected column has the correct CSS classes
        var selectedHeader = document.QuerySelector("th.selected-column-header") as IHtmlTableHeaderCellElement;
        Assert.NotNull(selectedHeader);
        Assert.Equal("Value", selectedHeader.TextContent.Trim());

        var selectedCells = document.QuerySelectorAll("td.selected-column-cell").Cast<IHtmlTableDataCellElement>().ToList();
        Assert.True(selectedCells.Count >= 2, "Should have at least 2 highlighted cells for test data");
        Assert.Contains(selectedCells, cell => cell.TextContent.Contains("123"));
        Assert.Contains(selectedCells, cell => cell.TextContent.Contains("456"));
    }

    [Fact]
    public async Task PreviewPane_ClickableCells_HaveCorrectOnclickAttributes()
    {
        using var sessionClient = _factory.CreateClient();

        // Setup test data
        var csvContent = "Col1,Col2,Col3\nA,B,C\nD,E,F";
        await UploadTestFileAsync(sessionClient, "filename", "clickable-test.csv", csvContent);
        await LoadDataAsync(sessionClient, "clickable-test.csv");

        // Get preview without selection
        var previewHtml = await GetPreviewAsync(sessionClient);

        var config = Configuration.Default;
        var context = BrowsingContext.New(config);
        var document = await context.OpenAsync(req => req.Content(previewHtml));

        // Verify all headers have onclick attributes
        var headers = document.QuerySelectorAll("th").Cast<IHtmlTableHeaderCellElement>().ToList();
        foreach (var header in headers)
        {
            var onclick = header.GetAttribute("onclick");
            Assert.NotNull(onclick);
            Assert.StartsWith("selectColumn('", onclick);
            Assert.EndsWith("')", onclick);
        }

        // Verify all data cells have onclick attributes
        var cells = document.QuerySelectorAll("td").Cast<IHtmlTableDataCellElement>().ToList();
        foreach (var cell in cells)
        {
            var onclick = cell.GetAttribute("onclick");
            Assert.NotNull(onclick);
            Assert.StartsWith("selectColumn('", onclick);
            Assert.EndsWith("')", onclick);
        }

        // Verify cursor pointer style is applied
        Assert.Contains("cursor: pointer", previewHtml);
    }

    [Fact]
    public async Task PreviewPane_NavigationButtons_WorkCorrectly()
    {
        using var sessionClient = _factory.CreateClient();

        // Setup test data
        var csvContent = "Field1,Field2,Field3,Field4\n1,2,3,4\n5,6,7,8";
        await UploadTestFileAsync(sessionClient, "filename", "nav-test.csv", csvContent);
        await LoadDataAsync(sessionClient, "nav-test.csv");

        // Test step 0 - Previous button should be disabled
        var step0Html = await GetPreviewAsync(sessionClient, 0, "Field1");
        var config = Configuration.Default;
        var context = BrowsingContext.New(config);
        var step0Doc = await context.OpenAsync(req => req.Content(step0Html));

        var prevBtn = step0Doc.QuerySelector("#prevBtn") as IHtmlButtonElement;
        Assert.NotNull(prevBtn);
        Assert.True(prevBtn.HasAttribute("disabled"));

        var nextBtn = step0Doc.QuerySelector("#nextBtn") as IHtmlButtonElement;
        Assert.NotNull(nextBtn);
        Assert.False(nextBtn.HasAttribute("disabled")); // Should be enabled when column is selected

        // Test step 1 - Both buttons should be enabled
        var step1Html = await GetPreviewAsync(sessionClient, 1, "Field2");
        var step1Doc = await context.OpenAsync(req => req.Content(step1Html));

        var step1PrevBtn = step1Doc.QuerySelector("#prevBtn") as IHtmlButtonElement;
        var step1NextBtn = step1Doc.QuerySelector("#nextBtn") as IHtmlButtonElement;
        Assert.NotNull(step1PrevBtn);
        Assert.NotNull(step1NextBtn);
        Assert.False(step1PrevBtn.HasAttribute("disabled"));
        Assert.False(step1NextBtn.HasAttribute("disabled"));

        // Verify mapping progress text
        var mappingProgress = step1Doc.QuerySelector(".mapping-progress");
        Assert.NotNull(mappingProgress);
        Assert.Contains("Step 2", mappingProgress.TextContent);
    }

    [Fact]
    public async Task PreviewPane_NoColumnSelected_NextButtonDisabled()
    {
        using var sessionClient = _factory.CreateClient();

        // Setup test data
        var csvContent = "A,B,C\n1,2,3";
        await UploadTestFileAsync(sessionClient, "filename", "button-test.csv", csvContent);
        await LoadDataAsync(sessionClient, "button-test.csv");

        // Get preview with no column selected
        var previewHtml = await GetPreviewAsync(sessionClient, 0, "");

        var config = Configuration.Default;
        var context = BrowsingContext.New(config);
        var document = await context.OpenAsync(req => req.Content(previewHtml));

        // Next button should be disabled when no column is selected
        var nextBtn = document.QuerySelector("#nextBtn") as IHtmlButtonElement;
        Assert.NotNull(nextBtn);
        Assert.True(nextBtn.HasAttribute("disabled"));
    }

    [Fact]
    public async Task PreviewPane_NoDataLoaded_ShowsWarningMessage()
    {
        using var sessionClient = _factory.CreateClient();

        // Try to get preview without loading any data first
        var previewHtml = await GetPreviewAsync(sessionClient);

        var config = Configuration.Default;
        var context = BrowsingContext.New(config);
        var document = await context.OpenAsync(req => req.Content(previewHtml));

        // Should show warning message
        var alertWarning = document.QuerySelector(".alert-warning");
        Assert.NotNull(alertWarning);
        Assert.Contains("Load data first", alertWarning.TextContent);

        // Should not show preview table
        var previewTable = document.QuerySelector("#previewTable");
        Assert.Null(previewTable);
    }

    [Theory]
    [InlineData("Field1")]
    [InlineData("Field2")]
    [InlineData("Field3")]
    public async Task PreviewPane_SelectDifferentColumns_HighlightsCorrectColumn(string columnToSelect)
    {
        using var sessionClient = _factory.CreateClient();

        // Setup test data with multiple columns
        var csvContent = "Field1,Field2,Field3\nValue1A,Value2A,Value3A\nValue1B,Value2B,Value3B\nValue1C,Value2C,Value3C";
        await UploadTestFileAsync(sessionClient, "filename", "multi-column-test.csv", csvContent);
        await LoadDataAsync(sessionClient, "multi-column-test.csv");

        // Select the specified column
        var previewHtml = await GetPreviewAsync(sessionClient, 0, columnToSelect);

        var config = Configuration.Default;
        var context = BrowsingContext.New(config);
        var document = await context.OpenAsync(req => req.Content(previewHtml));

        // Verify exactly one header is highlighted and it's the correct one
        var selectedHeaders = document.QuerySelectorAll("th.selected-column-header").Cast<IHtmlTableHeaderCellElement>().ToList();
        Assert.Single(selectedHeaders);
        Assert.Equal(columnToSelect, selectedHeaders[0].TextContent.Trim());

        // Verify all other headers are NOT highlighted
        var allHeaders = document.QuerySelectorAll("th").Cast<IHtmlTableHeaderCellElement>().ToList();
        var unselectedHeaders = allHeaders.Where(h => h.TextContent.Trim() != columnToSelect).ToList();
        foreach (var header in unselectedHeaders)
        {
            Assert.False(header.ClassList.Contains("selected-column-header"));
        }

        // Verify data cells in selected column are highlighted
        var selectedCells = document.QuerySelectorAll("td.selected-column-cell").Cast<IHtmlTableDataCellElement>().ToList();
        Assert.True(selectedCells.Count >= 3, $"Should have at least 3 highlighted cells for {columnToSelect} column");

        // Verify selected column display in UI
        var selectedColumnSpan = document.QuerySelector(".selected-column");
        Assert.NotNull(selectedColumnSpan);
        Assert.Equal(columnToSelect, selectedColumnSpan.TextContent.Trim());
    }
}