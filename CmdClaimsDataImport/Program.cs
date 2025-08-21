using CmdClaimsDataImport;

try
{
    var cmdArgs = Environment.GetCommandLineArgs().Skip(1).ToArray();
    var arguments = ArgumentParser.Parse(cmdArgs);
    await ImportProcessor.ProcessImportAsync(arguments).ConfigureAwait(false);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    Environment.Exit(1);
}
