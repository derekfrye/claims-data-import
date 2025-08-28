using HtmlClaimsDataImport.Application.Interfaces;
using HtmlClaimsDataImport.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Check for custom temp directory argument
string? customTempDir = null;
if (args.Length > 0 && args[0].StartsWith("--temp-dir=", StringComparison.Ordinal))
{
    customTempDir = args[0]["--temp-dir=".Length..];
    if (!string.IsNullOrEmpty(customTempDir))
    {
        TempDirectoryCleanupService.SetTempBasePath(customTempDir);
        Console.WriteLine($"Using custom temp directory: {customTempDir}");
    }
}

// Add services to the container.
builder.Services.AddRazorPages()
    .AddMvcOptions(o => o.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));
builder.Services.AddSession();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHostedService<TempDirectoryCleanupService>();

// Register Mediator (source generator adds handlers and DI setup)
builder.Services.AddMediator();

// Register Clean Architecture services
builder.Services.AddScoped<IFileUploadService, FileUploadService>();
builder.Services.AddScoped<IValidationService, ValidationService>();
builder.Services.AddScoped<IDataImportService, DataImportService>();
builder.Services.AddScoped<IPreviewService, PreviewService>();
builder.Services.AddScoped<IMappingTranslationService, MappingTranslationService>();
builder.Services.AddScoped<IAICompletionService, AICompletionService>();
builder.Services.AddScoped<IConfigService, ConfigService>();

// Register session-scoped temp directory service
builder.Services.AddScoped<ITempDirectoryService>(provider =>
{
    IHttpContextAccessor httpContextAccessor = provider.GetRequiredService<IHttpContextAccessor>();
    HttpContext? httpContext = httpContextAccessor.HttpContext;

    // Ensure session is loaded/initialized before getting ID
    ISession? session = httpContext?.Session;
    if (session != null)
    {
        // This ensures the session is loaded and has an ID
        _ = session.IsAvailable;
    }

    var sessionId = session?.Id ?? httpContext?.TraceIdentifier ?? "default";
    var basePath = TempDirectoryCleanupService.GetTempBasePath();

    var service = new TempDirectoryService(sessionId, basePath);
    TempDirectoryCleanupService.RegisterService(service);
    return service;
});

WebApplication app = builder.Build();

// Add additional cleanup for when the app is forcefully terminated
AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
{
    Console.WriteLine("ProcessExit: Cleaning up temp directories...");
    TempDirectoryCleanupService.CleanupAllDirectories();
};

Console.CancelKeyPress += (sender, e) =>
{
    Console.WriteLine("Ctrl+C: Cleaning up temp directories...");
    TempDirectoryCleanupService.CleanupAllDirectories();
};

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    _ = app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    _ = app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseSession();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();

/// <summary>
/// Partial class for Program to support minimal API hosting.
/// </summary>
public partial class Program { }
