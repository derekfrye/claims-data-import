namespace HtmlClaimsDataImport.Application.Commands;

using MediatR;

public record ClearMappingCommand(string tmpDir, string outputColumn) : IRequest<bool>;

