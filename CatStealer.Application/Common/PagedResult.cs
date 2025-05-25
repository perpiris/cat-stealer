namespace CatStealer.Application.Common;

public class PagedResult<T>
{
    public required List<T> Items { get; set; }
    
    public int CurrentPage { get; set; }
    
    public int TotalPages { get; set; }
    
    public int PageSize { get; set; }
}