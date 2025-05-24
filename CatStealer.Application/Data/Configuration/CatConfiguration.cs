using CatStealer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CatStealer.Application.Data.Configuration;

public class CatConfiguration : IEntityTypeConfiguration<Cat>
{
    public void Configure(EntityTypeBuilder<Cat> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .ValueGeneratedOnAdd();

        builder.Property(c => c.CatId)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.HasIndex(c => c.CatId)
            .IsUnique();

        builder.Property(c => c.Width)
            .IsRequired();

        builder.Property(c => c.Height)
            .IsRequired();

        builder.Property(c => c.Image)
            .IsRequired();

        builder.Property(c => c.Created)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");
        
        builder.HasMany(c => c.Tags)
            .WithMany(t => t.Cats);
    }
}