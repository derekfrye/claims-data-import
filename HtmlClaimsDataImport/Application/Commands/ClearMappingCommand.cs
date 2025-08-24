namespace HtmlClaimsDataImport.Application.Commands;

using Mediator;

public record ClearMappingCommand(string tmpDir, string outputColumn) : IRequest<bool>;
