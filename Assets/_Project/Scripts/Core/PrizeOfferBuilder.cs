using System.Collections.Generic;
using UnityEngine;

/// <summary>Arma las tarjetas de premio que se ofrecen al ganar una ronda.</summary>
public static class PrizeOfferBuilder
{
    public static List<PrizeData> Build(IReadOnlyList<PrizeData> pool, int round, int lives, BalanceConfig balance)
    {
        var offers = new List<PrizeData>();
        if (pool == null || balance == null) return offers;

        var normals = new List<PrizeData>();
        var poisons = new List<PrizeData>();
        foreach (PrizeData prize in pool)
        {
            if (prize == null) continue;
            if (prize.IsPoisoned) poisons.Add(prize);
            else if (FindByType(normals, prize.Type) == null) normals.Add(prize);
        }

        // Con las vidas al máximo el corazón no sirve: se quita si quedan suficientes opciones.
        if (lives >= balance.MaxLives && normals.Count > balance.PrizeChoices)
            normals.RemoveAll(p => p.Type == PrizeType.ExtraHeart);

        Shuffle(normals);
        int count = Mathf.Min(balance.PrizeChoices, normals.Count);
        for (int i = 0; i < count; i++) offers.Add(normals[i]);

        if (round >= balance.FirstPoisonRound && poisons.Count > 0 && offers.Count > 0
            && Random.value < balance.PoisonChance)
        {
            InsertPoison(offers, poisons);
        }
        return offers;
    }

    private static void InsertPoison(List<PrizeData> offers, List<PrizeData> poisons)
    {
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
