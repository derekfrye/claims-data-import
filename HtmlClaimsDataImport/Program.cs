var builder = WebApplication.CreateBuilder(args);

// Check for custom temp directory argument
if (args.Length > 0 && args[0].StartsWith("--temp-dir="))
{
    var tempDir = args[0].Substring("--temp-dir=".Length);
    if (!string.IsNullOrEmpty(tempDir))
    {
        HtmlClaimsDataImport.Services.TempDirectoryCleanupService.SetTempBasePath(tempDir);
        Console.WriteLine($"Using custom temp directory: {tempDir}");
    }
}

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddHostedService<HtmlClaimsDataImport.Services.TempDirectoryCleanupService>();

var app = builder.Build();

// Add additional cleanup for when the app is forcefully terminated
AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
{
    Console.WriteLine("ProcessExit: Cleaning up temp directories...");
    HtmlClaimsDataImport.Services.TempDirectoryCleanupService.CleanupAllDirectories();
};

Console.CancelKeyPress += (sender, e) =>
{
    Console.WriteLine("Ctrl+C: Cleaning up temp directories...");
    HtmlClaimsDataImport.Services.TempDirectoryCleanupService.CleanupAllDirectories();
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

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();

public partial class Program { }
