using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using ReactBattleArena.Application.Characters.Commands;
using ReactBattleArena.Application.Common;
using System.Reflection;
using System.Runtime.ConstrainedExecution;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Model;

namespace ReactBattleArena.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        return services;
    }
}

//Ne yapar?

//MediatR → Handler’ları bulur(CreateCharacterCommandHandler)
//FluentValidation → Validator’ları bulur(CreateCharacterCommandValidator)
//Pipeline → Her istekte validation çalışır