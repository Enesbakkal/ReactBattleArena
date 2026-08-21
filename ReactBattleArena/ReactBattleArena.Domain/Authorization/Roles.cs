using System.Numerics;

namespace ReactBattleArena.Domain.Authorization;

public static class Roles
{
    public const string Admin = "Admin";
    public const string Player = "Player";
    public const string ShopOwner = "ShopOwner";

}

//Authorization(roller)
//Authentication = kimsin? (JWT)
//Authorization = ne yapabilirsin? (rol)

//Rol Yetki(şimdilik)
//Player
//Login, katalog GET, kendi profili
//Admin
//Character Create / Update / Delete
//Register → varsayılan Player.İlk Admin’i SSMS’te Role = Admin yaparak veririz (basit).