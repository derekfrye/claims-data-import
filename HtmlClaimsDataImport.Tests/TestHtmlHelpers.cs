using System.Net.Http;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Html.Dom;
using Xunit;

namespace HtmlClaimsDataImport.Tests;

public static class TestHtmlHelpers
{
    public static async Task<string> GetAntiForgeryTokenAsync(HttpClient client, string path = "/ClaimsDataImporter")
    {
        var response = await client.GetAsync(path);
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();

        var doc = await ParseDocumentAsync(html);
        var tokenInput = doc.QuerySelector("input[name='__RequestVerificationToken']") as IHtmlInputElement;
        Assert.NotNull(tokenInput);
        return tokenInput!.Value;
    }

    public static async Task<IHtmlDocument> ParseDocumentAsync(string html)
    {
        var context = BrowsingContext.New(Configuration.Default);
        var document = await context.OpenAsync(req => req.Content(html));
        return (IHtmlDocument)document;
    }
}

