using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReactBattleArena.Domain.Authorization;
using ReactBattleArena.Domain.Users;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ReactBattleArena.Infrastructure.Persistence;

public sealed class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("UserRoles");
        builder.HasKey(x => new { x.UserId, x.RoleId });

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        //Cascade (User): kullanıcı silinince o kullanıcının UserRoles satırları da silinsin.

        builder.HasOne<Role>()
            .WithMany()
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Restrict);
        // Restrict(Role): rol hâlâ birine bağlıysa rolü silemezsin.
    }
}

//“UserRole’un bir User’ı var; bir User’ın çok UserRole’u olabilir; FK kolonu UserId.” WithMany() boş çünkü User sınıfına ICollection<UserRole> yazmadık; ilişki yine durur. Guid