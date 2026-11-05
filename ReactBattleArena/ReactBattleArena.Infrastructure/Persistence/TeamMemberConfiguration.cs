using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReactBattleArena.Domain.Characters;
using ReactBattleArena.Domain.Teams;

namespace ReactBattleArena.Infrastructure.Persistence;

public sealed class TeamMemberConfiguration : IEntityTypeConfiguration<TeamMember>
{
    public void Configure(EntityTypeBuilder<TeamMember> builder)
    {
        builder.ToTable("TeamMembers");

        // Bir yerde tek karakter; aynı slot iki kez dolmaz.
        builder.HasKey(x => new { x.TeamId, x.SlotIndex });

        // Aynı karakter aynı takımda iki slot’ta olmasın.
        builder.HasIndex(x => new { x.TeamId, x.CharacterId }).IsUnique();

        builder.Property(x => x.SlotIndex).IsRequired();

        builder.HasOne<Team>()
            .WithMany()
            .HasForeignKey(x => x.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Character>()
            .WithMany()
            .HasForeignKey(x => x.CharacterId)
            .OnDelete(DeleteBehavior.Restrict);
        // Karakter katalogda dururken üyelik satırı varken silinmesin (Restrict).
    }
}