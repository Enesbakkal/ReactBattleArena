using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using ReactBattleArena.Application.Common;
using System.Reflection;

namespace ReactBattleArena.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Application assembly'sindeki tüm IRequestHandler<,> implementasyonlarını tarar ve DI'a ekler.
        // DeleteCharacterCommandHandler da burada kayıt olur — elle AddScoped yazmana gerek yok.
        // İlgili handler yoksa Send çağrısında "handler bulunamadı" hatası alırsın.
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

        // FluentValidation → Validator'ları bulur (CreateCharacterCommandValidator vb.)
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        // Pipeline → Her istekte validation çalışır (ValidationBehavior)
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        return services;
    }
}