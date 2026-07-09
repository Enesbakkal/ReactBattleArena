using MediatR;

namespace ReactBattleArena.Application.Characters.Commands;

// MediatR, command tipine göre handler'ı DI'dan bulur. Özel bir "yakalama" yok; tip eşleşmesi vardır.
public sealed record DeleteCharacterCommand(Guid Id) : IRequest<bool>;
