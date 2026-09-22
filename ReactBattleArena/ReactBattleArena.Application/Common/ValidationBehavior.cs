using FluentValidation;
using MediatR;

namespace ReactBattleArena.Application.Common;

public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await next();// Hata yoksa handler'a geç

        var context = new ValidationContext<TRequest>(request);

        var failures = _validators
            .Select(v => v.Validate(context))
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Count != 0)
            throw new ValidationException(failures);

        return await next();
    }
}

// Ne yapar? Handler çalışmadan önce validator kurallarını çalıştırır; hata varsa ValidationException fırlatır.
// ValidationBehavior bu kuralları handler'dan önce çalıştırır. Hata varsa ValidationException atar ve next() çağrılmaz. Users tablosuna satır gitmez.