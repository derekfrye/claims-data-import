using Microsoft.AspNetCore.Mvc.Testing;

namespace HtmlClaimsDataImport.Tests;

[CollectionDefinition("WebApp")]
public class SharedWebApplicationCollection : ICollectionFixture<WebApplicationFactory<Program>>
{
}