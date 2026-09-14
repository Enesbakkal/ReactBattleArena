using MediatR;
using ReactBattleArena.Application.Authentication.Commands;

namespace ReactBattleArena.Application.Commands;

public sealed record RefreshCommand(string RefreshToken) : IRequest<LoginResult?>;
// null → fiş yok / iptal edilmiş / süresi bitmiş → controller 401 döner.
// Dönüş tipi login ile aynı LoginResult?, çünkü cevap yine yeni access + yeni refresh çifti olacak.
// Şifre alanı yok; kullanıcı burada kimliğini fişle kanıtlıyor.