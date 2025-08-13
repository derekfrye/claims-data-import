using LibClaimsDataImport.Importer;
using Sylvan.Data.Csv;

namespace CmdClaimsDataImport;

public static class ImportProcessor
{
    public static async Task ProcessImportAsync(ImportArguments arguments)
    {
        Console.WriteLine($"Processing CSV file: {arguments.CsvFileName}");
        Console.WriteLine($"Target database: {arguments.DatabasePath}");
        Console.WriteLine($"Target table: {arguments.TableName}");

        using var streamReader = new StreamReader(arguments.CsvFileName);
        
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

        // Load config if specified, otherwise use default
        LibClaimsDataImport.Importer.ImportConfig? config = null;
        if (!string.IsNullOrEmpty(arguments.ConfigPath))
        {
            config = LibClaimsDataImport.Importer.ImportConfig.LoadFromFile(arguments.ConfigPath);
        }

        // Create File instance and import data
        var file = LibClaimsDataImport.Importer.File.New(streamReader, fileSpec, config);
        Console.WriteLine("Importing data to database...");
        await file.WriteToDb(arguments.DatabasePath, arguments.TableName);
        
        Console.WriteLine("Import completed successfully!");
    }
}