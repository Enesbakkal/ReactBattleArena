namespace ReactBattleArena.Api.Contracts;

public sealed class RenameTeamRequest
{
    public string Name { get; set; } = string.Empty;
}

// JSON gövdesindeki name buraya bağlanır. Command’a giden Name budur; TeamId ve OwnerUserId URL ile JWT’den gelir, istemcinin “ben başkasının adına yazıyorum” demesine izin verilmez.