namespace CatStealer.Domain.Entities;

public class Cat
{
    public int Id { get; set; }
    
    public required string CatId { get; set; } 
    
    public int Width { get; set; } 
    
    public int Height { get; set; } 
    
    public required string Image { get; set; }

    public DateTime Created { get; set; }

    public ICollection<Tag> Tags { get; set; } = new List<Tag>();
}