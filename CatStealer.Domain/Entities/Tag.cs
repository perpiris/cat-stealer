namespace CatStealer.Domain.Entities;

public class Tag
{
    public int Id { get; set; }
    
    public required string Name { get; set; }
    
    public DateTime Created { get; set; }
    
    public ICollection<Cat> Cats { get; set; } = new List<Cat>();
}