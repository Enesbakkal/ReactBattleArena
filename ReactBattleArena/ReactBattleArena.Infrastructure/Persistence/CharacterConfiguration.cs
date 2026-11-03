using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Configuration;
using ReactBattleArena.Domain.Characters;
using System.Runtime.ConstrainedExecution;

namespace ReactBattleArena.Infrastructure.Persistence;

public sealed class CharacterConfiguration : IEntityTypeConfiguration<Character>
{
    public void Configure(EntityTypeBuilder<Character> builder)
    {
        builder.ToTable("Characters");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Universe).IsRequired().HasMaxLength(120);
        builder.Property(x => x.Biography).HasMaxLength(2000);
        builder.Property(x => x.ImageUrl).HasMaxLength(500);

        builder.HasIndex(x => x.Universe);
        builder.HasIndex(x => new { x.Universe, x.Name });
        // EF convention: entity’deki int property’ler satır yazmadan kolona map olur. Configuration yalnız özel kurallar içindir: MaxLength, IsRequired string’de, index.

        builder.Property(x => x.BaseHealth).IsRequired().HasDefaultValue(0);
        builder.Property(x => x.BaseTechnique).IsRequired().HasDefaultValue(0);
        builder.Property(x => x.BaseAccuracy).IsRequired().HasDefaultValue(0);
        builder.Property(x => x.BaseForesight).IsRequired().HasDefaultValue(0);
    }
}