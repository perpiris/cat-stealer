using CatStealer.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CatStealer.Application.Data;

public class DataContext : DbContext
{
    public DbSet<Cat> Cats { get; set; }
    public DbSet<Tag> Tags { get; set; }
    
    public DataContext(DbContextOptions<DataContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        builder.ApplyConfigurationsFromAssembly(typeof(DataContext).Assembly);
    }
}