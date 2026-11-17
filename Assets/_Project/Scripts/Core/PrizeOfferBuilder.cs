using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Arma las tarjetas de premio que se ofrecen al ganar una ronda. Cada tarjeta sortea su rareza según la ronda
/// (los raros y épicos salen más a medida que el juego se pone difícil); ganar la apuesta hace que las tres sean raras o épicas.
/// Un premio ya elegido no vuelve a salir: cada premiación trae ventajas nuevas. El corazón es la excepción
/// (se repite, pero no en dos premiaciones seguidas ni con las vidas llenas). Si ya no quedan premios, salen menos tarjetas.
/// </summary>
public static class PrizeOfferBuilder
{
    public static List<PrizeData> Build(IReadOnlyList<PrizeData> pool, int round, int lives,
        PlayerModifiers modifiers, bool betWon, BalanceConfig balance)
    {
        var offers = new List<PrizeData>();
        if (pool == null || balance == null) return offers;

        var normals = new List<PrizeData>();
        var poisons = new List<PrizeData>();
        foreach (PrizeData prize in pool)
        {
            if (prize == null) continue;
            if (prize.IsPoisoned) poisons.Add(prize);
            else if (FindByType(normals, prize.Type) == null && IsUnlocked(prize.Rarity, round, balance)) normals.Add(prize);
        }

        bool livesFull = lives >= balance.MaxLives;
        List<PrizeData> available = normals.FindAll(p => IsOfferable(p, modifiers, livesFull, true));
        // Casi sin premios nuevos: el corazón puede repetirse aunque se haya elegido en la premiación anterior.
        if (available.Count < balance.PrizeChoices) available = normals.FindAll(p => IsOfferable(p, modifiers, livesFull, false));

        float epic = balance.GetEpicChance(round);
        float rare = balance.GetRareChance(round);
        int count = Mathf.Min(balance.PrizeChoices, available.Count);
        for (int i = 0; i < count; i++)
        {
            PrizeRarity wanted = RollRarity(epic, rare);
            // Apuesta ganada: las tres tarjetas son raras o épicas (si quedan).
            if (betWon && wanted == PrizeRarity.Common) wanted = PrizeRarity.Rare;
            PrizeData pick = TakeClosest(available, wanted);
            if (pick == null) break;
            offers.Add(pick);
        }
        Shuffle(offers);

        if (round >= balance.FirstPoisonRound && poisons.Count > 0 && offers.Count > 0
            && Random.value < balance.PoisonChance)
        {
            InsertPoison(offers, poisons.FindAll(p => IsOfferable(p, modifiers, livesFull, true)), !betWon);
        }
        return offers;
    }

    private static bool IsUnlocked(PrizeRarity rarity, int round, BalanceConfig balance) => rarity switch
    {
        PrizeRarity.Epic => round >= balance.EpicFromRound,
        PrizeRarity.Rare => round >= balance.RareFromRound,
        _ => true,
    };

    private static bool IsMaxed(PrizeData prize, PlayerModifiers modifiers) =>
        modifiers != null && modifiers.GetLevel(prize.Type) >= prize.MaxLevel;

    private static bool IsOfferable(PrizeData prize, PlayerModifiers modifiers, bool livesFull, bool avoidRepeat)
    {
        if (IsMaxed(prize, modifiers)) return false;
        if (prize.Type == PrizeType.ExtraHeart)
            return !livesFull && !(avoidRepeat && modifiers != null && modifiers.WasPickedLastScreen(PrizeType.ExtraHeart));
        // El Súper imán mejora al Imán: solo sale si ya lo tiene.
        if (prize.Type == PrizeType.SuperMagnet) return modifiers != null && modifiers.Has(PrizeType.Magnet);
        return true;
    }

    private static PrizeRarity RollRarity(float epicChance, float rareChance)
    {
        float roll = Random.value;
        if (roll < epicChance) return PrizeRarity.Epic;
        return roll < epicChance + rareChance ? PrizeRarity.Rare : PrizeRarity.Common;
    }

    // Saca un premio de la rareza pedida; si no hay, de la más cercana (primero hacia abajo).
    private static PrizeData TakeClosest(List<PrizeData> available, PrizeRarity wanted)
    {
        int target = (int)wanted;
        int maxRarity = (int)PrizeRarity.Epic;
        for (int distance = 0; distance <= maxRarity; distance++)
        {
            PrizeData pick = TakeRandom(available, target - distance);
            if (pick == null && distance > 0) pick = TakeRandom(available, target + distance);
            if (pick != null) return pick;
        }
        return null;
    }

    private static PrizeData TakeRandom(List<PrizeData> available, int rarity)
    {
        List<PrizeData> matches = available.FindAll(p => (int)p.Rarity == rarity);
        if (matches.Count == 0) return null;
        PrizeData pick = matches[Random.Range(0, matches.Count)];
        available.Remove(pick);
        return pick;
    }

    // allowReplace = false: solo envenena una tarjeta que ya está (así no cambia una rara por una común).
    private static void InsertPoison(List<PrizeData> offers, List<PrizeData> poisons, bool allowReplace)
    {
        if (poisons.Count == 0) return;
        // Mejor envenenar uno que ya está en la oferta: así se ve igual que el normal.
        var slots = new List<int>();
        for (int i = 0; i < offers.Count; i++)
        {
            if (FindByType(poisons, offers[i].Type) != null) slots.Add(i);
        }
        if (slots.Count > 0)
        {
            int slot = slots[Random.Range(0, slots.Count)];
            offers[slot] = FindByType(poisons, offers[slot].Type);
            return;
        }

        // Si no, cambiar una tarjeta al azar por un envenenado de un tipo que no esté.
        if (!allowReplace) return;
        List<PrizeData> free = poisons.FindAll(p => FindByType(offers, p.Type) == null);
        if (free.Count == 0) return;
        offers[Random.Range(0, offers.Count)] = free[Random.Range(0, free.Count)];
    }

    private static PrizeData FindByType(List<PrizeData> list, PrizeType type)
    {
        foreach (PrizeData prize in list)
        {
            if (prize.Type == type) return prize;
        }
        return null;
    }

    private static void Shuffle(List<PrizeData> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
