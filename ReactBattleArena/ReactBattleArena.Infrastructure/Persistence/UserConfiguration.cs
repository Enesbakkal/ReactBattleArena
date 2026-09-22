using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReactBattleArena.Domain.Users;

namespace ReactBattleArena.Infrastructure.Persistence;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserName).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Email).IsRequired().HasMaxLength(200);
        builder.Property(x => x.DisplayName).HasMaxLength(100);

        builder.HasIndex(x => x.UserName).IsUnique();
        builder.HasIndex(x => x.Email).IsUnique();
        builder.Property(x => x.PasswordHash).IsRequired().HasMaxLength(500);
        //UserName ve Email unique — aynı kullanıcı / mail iki kez eklenemez.
        // EF eşlemesi tabloyu Users diye açar. Kullanıcı adı ve e-posta unique'dir.
        // PasswordHash zorunludur ve en fazla 500 karakterdir.
        // Validator'daki 100 karakterlik parola sınırı düz metin içindir. Kolondaki 500, BCrypt string'i içindir.
    }
}