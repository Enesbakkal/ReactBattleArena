using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReactBattleArena.Domain.Battles;
using ReactBattleArena.Domain.Characters;

namespace ReactBattleArena.Infrastructure.Persistence;

public sealed class BattleFighterConfiguration : IEntityTypeConfiguration<BattleFighter>
{
    public void Configure(EntityTypeBuilder<BattleFighter> builder)
    {
        builder.ToTable("BattleFighters");

        builder.HasKey(x => new { x.BattleId, x.Side, x.SlotIndex });

        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Universe).IsRequired().HasMaxLength(100);

        builder.HasOne<Battle>()
            .WithMany()
            .HasForeignKey(x => x.BattleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Character>()
            .WithMany()
            .HasForeignKey(x => x.CharacterId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);
    }
}

// Anahtar üç parçalı: aynı dövüşte iki taraf da slot 0 kullanır; yalnız SlotIndex yetmez.
//Battle silinirse fighter’lar Cascade ile gider.Character Restrict: katalog satırı varken fighter kopyası varsa karakter silinmesin.
//Name / Universe zaten kopyada durduğu için geçmiş metin olarak okunur; Restrict ek güvenlik.