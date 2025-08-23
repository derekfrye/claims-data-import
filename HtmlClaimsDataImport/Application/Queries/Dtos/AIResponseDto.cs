namespace HtmlClaimsDataImport.Application.Queries.Dtos
{
    public class AIResponseDto
    {
        public string ResponseText { get; set; } = string.Empty;
        public bool IsSimulated { get; set; }
        public string Provider { get; set; } = "openai";
        public string Model { get; set; } = "gpt-4o-mini";
    }
}

