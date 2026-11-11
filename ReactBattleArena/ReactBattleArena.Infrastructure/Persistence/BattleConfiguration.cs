using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReactBattleArena.Domain.Battles;
using ReactBattleArena.Domain.Teams;
using ReactBattleArena.Domain.Users;

namespace ReactBattleArena.Infrastructure.Persistence;

public sealed class BattleConfiguration : IEntityTypeConfiguration<Battle>
{
    public void Configure(EntityTypeBuilder<Battle> builder)
    {
        builder.ToTable("Battles");
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.InitiatorUserId);
        builder.HasIndex(x => x.CreatedAtUtc);
        builder.HasIndex(x => x.IsHighlighted);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.InitiatorUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.OpponentUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasOne<Team>()
            .WithMany()
            .HasForeignKey(x => x.InitiatorTeamId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Team>()
            .WithMany()
            .HasForeignKey(x => x.OpponentTeamId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);
    }
}

//Restrict: kullanıcı veya takım silinirken geçmiş dövüş satırı otomatik silinmesin.Geçmiş bozulmasın diye Cascade seçmedik.

//OpponentUserId / OpponentTeamId için IsRequired(false): sistem diziliminde ikisi de boş kalabilir.

//Index’ler: geçmiş listesi InitiatorUserId + tarih ile okunur; highlights IsHighlighted ile süzülür.

//Enum’lar (BattleOutcome vb.) EF’e ekstra satır yazmadan int olarak gider.