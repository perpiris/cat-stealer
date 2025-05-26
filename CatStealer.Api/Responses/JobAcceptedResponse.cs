namespace CatStealer.Api.Responses;

public class JobAcceptedResponse
{
    public required string JobId { get; set; }
    
    public string? StatusUrl { get; set; }
}