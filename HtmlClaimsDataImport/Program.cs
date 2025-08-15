using HtmlClaimsDataImport.Services;

var builder = WebApplication.CreateBuilder(args);

// Check for custom temp directory argument
string? customTempDir = null;
if (args.Length > 0 && args[0].StartsWith("--temp-dir="))
{
    customTempDir = args[0].Substring("--temp-dir=".Length);
    if (!string.IsNullOrEmpty(customTempDir))
    {
        TempDirectoryCleanupService.SetTempBasePath(customTempDir);
        Console.WriteLine($"Using custom temp directory: {customTempDir}");
    }
}

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddSession();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHostedService<TempDirectoryCleanupService>();

// Register session-scoped temp directory service
builder.Services.AddScoped<ITempDirectoryService>(provider =>
{
    var httpContextAccessor = provider.GetRequiredService<IHttpContextAccessor>();
    var httpContext = httpContextAccessor.HttpContext;
    var sessionId = httpContext?.Session?.Id ?? httpContext?.TraceIdentifier ?? "default";
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

public partial class Program { }
