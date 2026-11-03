using System;
using System.Collections.Generic;
using System.Text;

namespace ReactBattleArena.Domain.Characters;
public sealed class Character
{
    private Character()
    {
        
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string Universe { get; private set; } = null!;
    public string? Biography { get; private set; }
    public int Rarity { get; private set; }
    public int BaseAttack { get; private set; }
    public int BaseDefense { get; private set; }
    public int BaseSpeed { get; private set; }
    public int BaseHealth { get; private set; }
    public int BaseTechnique { get; private set; }
    public int BaseAccuracy { get; private set; }
    public int BaseForesight { get; private set; }
    public string? ImageUrl { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public static Character Create( // factory
        string name,
        string universe,
        string? biography,
        int rarity,
        int baseAttack,
        int baseDefense,
        int baseSpeed,
        int baseHealth,
        int baseTechnique,
        int baseAccuracy,
        int baseForesight,
        string? imageUrl,
        DateTime utcNow)
    {
        return new Character
        {
            Id = Guid.NewGuid(),
            Name = name,
            Universe = universe,
            Biography = biography,
            Rarity = rarity,
            BaseAttack = baseAttack,
            BaseDefense = baseDefense,
            BaseSpeed = baseSpeed,
            BaseHealth = baseHealth,
            BaseTechnique = baseTechnique,
            BaseAccuracy = baseAccuracy,
            BaseForesight = baseForesight,
            ImageUrl = imageUrl,
            CreatedAtUtc = utcNow
        };
    }

    public void Update(
        string name,
        string universe,
        string? biography,
        int rarity,
        int baseAttack,
        int baseDefense,
        int baseSpeed,
        int baseHealth,
        int baseTechnique,
        int baseAccuracy,
        int baseForesight,
        string? imageUrl)
    {
        Name = name;
        Universe = universe;
        Biography = biography;
        Rarity = rarity;
        BaseAttack = baseAttack;
        BaseDefense = baseDefense;
        BaseSpeed = baseSpeed;
        BaseHealth = baseHealth;
        BaseTechnique = baseTechnique;
        BaseAccuracy = baseAccuracy;
        BaseForesight = baseForesight;
        ImageUrl = imageUrl;
    }
}

