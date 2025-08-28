namespace HtmlClaimsDataImport.Application.Commands;

using Mediator;

public record ClearMappingCommand(string TmpDir, string OutputColumn) : ICommand<bool>;
