namespace CmdClaimsDataImport;

public static class ArgumentParser
{
    public static ImportArguments Parse(string[] args)
    {
        if (args.Length == 0 || args.Contains("--help", StringComparer.Ordinal) || args.Contains("-h", StringComparer.Ordinal))
        {
            ShowUsage();
            Environment.Exit(0);
        }

        var arguments = new ImportArguments();

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--database":
                    if (i + 1 < args.Length)
                    {
                        arguments.DatabasePath = args[++i];
                    }
                    break;
                case "--table":
                    if (i + 1 < args.Length)
                    {
                        arguments.TableName = args[++i];
                    }
                    break;
                case "--filename":
                    if (i + 1 < args.Length)
                    {
                        arguments.CsvFileName = args[++i];
                    }
                    break;
                case "--config":
                    if (i + 1 < args.Length)
                    {
                        arguments.ConfigPath = args[++i];
                    }
                    break;
            }
        }

        ValidateArguments(arguments);
        return arguments;
    }

    private static void ValidateArguments(ImportArguments arguments)
    {
        if (string.IsNullOrWhiteSpace(arguments.DatabasePath))
        {
            Console.Error.WriteLine("Error: --database parameter is required");
            ShowUsage();
            Environment.Exit(1);
        }

        if (string.IsNullOrWhiteSpace(arguments.TableName))
        {
            Console.Error.WriteLine("Error: --table parameter is required");
            ShowUsage();
            Environment.Exit(1);
        }

        if (string.IsNullOrWhiteSpace(arguments.CsvFileName))
        {
            Console.Error.WriteLine("Error: --filename parameter is required");
            ShowUsage();
            Environment.Exit(1);
        }

        if (!File.Exists(arguments.CsvFileName))
        {
            Console.Error.WriteLine($"Error: File '{arguments.CsvFileName}' does not exist or is not readable");
            Environment.Exit(1);
        }
    }

    private static void ShowUsage()
    {
        Console.WriteLine("Claims Data Import Tool");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  CmdClaimsDataImport --database <path> --table <name> --filename <path> [--config <path>]");
        Console.WriteLine();
        Console.WriteLine("Arguments:");
        Console.WriteLine("  --database <path>    Path to SQLite database file (must be writable)");
        Console.WriteLine("  --table <name>       Name of existing table in the database");
        Console.WriteLine("  --filename <path>    Path to CSV file to import (must be readable)");
        Console.WriteLine("  --config <path>      Path to JSON configuration file (optional)");
        Console.WriteLine();
        Console.WriteLine("Example:");
        Console.WriteLine("  CmdClaimsDataImport --database claims.db --table claims_data --filename data.csv");
        Console.WriteLine("  CmdClaimsDataImport --database claims.db --table claims_data --filename data.csv --config myconfig.json");
    }
}
