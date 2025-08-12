using LibClaimsDataImport.Importer;
using Sylvan.Data.Csv;

// Parse command line arguments
var cmdArgs = Environment.GetCommandLineArgs().Skip(1).ToArray();

if (cmdArgs.Length == 0 || cmdArgs.Contains("--help") || cmdArgs.Contains("-h"))
{
    ShowUsage();
    return;
}

string? databasePath = null;
string? tableName = null;
string? csvFileName = null;

for (int i = 0; i < cmdArgs.Length; i++)
{
    switch (cmdArgs[i])
    {
        case "--database":
            if (i + 1 < cmdArgs.Length)
            {
                databasePath = cmdArgs[++i];
            }
            break;
        case "--table":
            if (i + 1 < cmdArgs.Length)
            {
                tableName = cmdArgs[++i];
            }
            break;
        case "--filename":
            if (i + 1 < cmdArgs.Length)
            {
                csvFileName = cmdArgs[++i];
            }
            break;
    }
}

if (string.IsNullOrWhiteSpace(databasePath))
{
    Console.Error.WriteLine("Error: --database parameter is required");
    ShowUsage();
    Environment.Exit(1);
}

if (string.IsNullOrWhiteSpace(tableName))
{
    Console.Error.WriteLine("Error: --table parameter is required");
    ShowUsage();
    Environment.Exit(1);
}

if (string.IsNullOrWhiteSpace(csvFileName))
{
    Console.Error.WriteLine("Error: --filename parameter is required");
    ShowUsage();
    Environment.Exit(1);
}

// Validate input file exists and is readable
if (!System.IO.File.Exists(csvFileName))
{
    Console.Error.WriteLine($"Error: File '{csvFileName}' does not exist or is not readable");
    Environment.Exit(1);
}

try
{
    Console.WriteLine($"Processing CSV file: {csvFileName}");
    Console.WriteLine($"Target database: {databasePath}");
    Console.WriteLine($"Target table: {tableName}");

    using var streamReader = new StreamReader(csvFileName);
    
    // Create CSV reader for scanning
    var scanCsvReader = CsvDataReader.Create(streamReader, new CsvDataReaderOptions { HasHeaders = true });
    
    // Create FileSpec and scan the file
    var fileSpec = new FileSpec(scanCsvReader);
    Console.WriteLine("Scanning CSV file to determine schema...");
    fileSpec.Scan();
    
    Console.WriteLine($"Detected {fileSpec.ColumnTypes.Count} columns:");
    foreach (var column in fileSpec.ColumnTypes)
    {
        Console.WriteLine($"  {column.Key}: {column.Value.Name}");
    }

    // Reset the stream for actual import
    streamReader.BaseStream.Seek(0, SeekOrigin.Begin);
    streamReader.DiscardBufferedData();

    // Create File instance and import data
    var file = LibClaimsDataImport.Importer.File.New(streamReader, fileSpec);
    Console.WriteLine("Importing data to database...");
    await file.WriteToDb(databasePath, tableName);
    
    Console.WriteLine("Import completed successfully!");
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    Environment.Exit(1);
}

static void ShowUsage()
{
    Console.WriteLine("Claims Data Import Tool");
    Console.WriteLine();
    Console.WriteLine("Usage:");
    Console.WriteLine("  CmdClaimsDataImport --database <path> --table <name> --filename <path>");
    Console.WriteLine();
    Console.WriteLine("Arguments:");
    Console.WriteLine("  --database <path>    Path to SQLite database file (must be writable)");
    Console.WriteLine("  --table <name>       Name of existing table in the database");
    Console.WriteLine("  --filename <path>    Path to CSV file to import (must be readable)");
    Console.WriteLine();
    Console.WriteLine("Example:");
    Console.WriteLine("  CmdClaimsDataImport --database claims.db --table claims_data --filename data.csv");
}
