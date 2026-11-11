using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReactBattleArena.Domain.Battles;

namespace ReactBattleArena.Infrastructure.Persistence;

public sealed class BattleEventConfiguration : IEntityTypeConfiguration<BattleEvent>
{
    public void Configure(EntityTypeBuilder<BattleEvent> builder)
    {
        builder.ToTable("BattleEvents");
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.BattleId, x.TurnNumber, x.Sequence });

        builder.HasOne<Battle>()
            .WithMany()
            .HasForeignKey(x => x.BattleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
// Index (BattleId, TurnNumber, Sequence): GET /api/battles/{id} olayları bu sırayla çeker. FE oynatma da aynı sırayı kullanır.
// Battle silinince olaylar Cascade ile gider.Özet + kopya + olaylar bir dövüş paketi.