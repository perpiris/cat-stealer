namespace CatStealer.Application.Common;

public class CaasCatImage
{
    public required string Id { get; set; }
    
    public required string Url { get; set; }
    
    public int Width { get; set; }
    
    public int Height { get; set; }
    
    public List<CaasBreed>? Breeds { get; set; }
}