using HtmlClaimsDataImport.Application.Interfaces;
using HtmlClaimsDataImport.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Check for custom temp directory argument
string? customTempDir = null;
if (args.Length > 0 && args[0].StartsWith("--temp-dir=", StringComparison.Ordinal))
{
    customTempDir = args[0].Substring("--temp-dir=".Length);
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

// Register MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

// Register Clean Architecture services
builder.Services.AddScoped<IFileUploadService, FileUploadService>();
builder.Services.AddScoped<IValidationService, ValidationService>();
builder.Services.AddScoped<IDataImportService, DataImportService>();
builder.Services.AddScoped<IPreviewService, PreviewService>();
builder.Services.AddScoped<IMappingTranslationService, MappingTranslationService>();
builder.Services.AddScoped<IAICompletionService, AICompletionService>();

// Register session-scoped temp directory service
builder.Services.AddScoped<ITempDirectoryService>(provider =>
{
    var httpContextAccessor = provider.GetRequiredService<IHttpContextAccessor>();
    var httpContext = httpContextAccessor.HttpContext;

    // Ensure session is loaded/initialized before getting ID
    var session = httpContext?.Session;
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

var app = builder.Build();

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
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
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
