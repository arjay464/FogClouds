using UnityEngine;
using FogClouds;

// Pact of the Devil — permanent +1 damage on attacks, +1 shield at turn start, +1 draw per turn
public class PactOfTheDevilPassive : IPassiveEffect
{
    public bool IsInteractive => false;

    public void OnAcquire(PlayerState player, GameState state)
    {
        Debug.Log($"[PactOfTheDevil] Player {player.PlayerId} made the pact.");
    }

    public void OnTurnStart(PlayerState player, GameState state)
    {
        player.GainShield(1);
        player.DrawCards(1, state.Rng);
        Debug.Log($"[PactOfTheDevil] Player {player.PlayerId} gained 1 shield and drew 1 card.");
    }

    public void OnDamageTaken(PlayerState player, int amount, GameState state) { }
    public void OnCardPlayed(PlayerState player, CardInstance card, GameState state) { }
}

// Blessing of Valor — all attacks deal +1 damage (permanent)
public class BlessingOfValorPassive : IPassiveEffect
{
    public bool IsInteractive => false;
    public void OnAcquire(PlayerState player, GameState state) { }
    public void OnTurnStart(PlayerState player, GameState state) { }
    public void OnDamageTaken(PlayerState player, int amount, GameState state) { }
    public void OnCardPlayed(PlayerState player, CardInstance card, GameState state) { }
}

// Blessing of Clarity — draw 3 extra cards on first turn start, then does nothing
public class BlessingOfClarityPassive : IPassiveEffect
{
    public bool IsInteractive => false;

    public void OnAcquire(PlayerState player, GameState state)
    {
        // Mark as not yet triggered using StackCount — 0 = not fired, 1 = fired
    }

    public void OnTurnStart(PlayerState player, GameState state)
    {
        var passive = player.Passives.Find(p => p.PassiveId == "blessing_of_clarity");
        if (passive != null && passive.StackCount == 0)
        {
            player.DrawCards(3, state.Rng);
            passive.StackCount = 1;
            Debug.Log($"[BlessingOfClarity] Player {player.PlayerId} drew 3 extra cards.");
        }
    }

    public void OnDamageTaken(PlayerState player, int amount, GameState state) { }
    public void OnCardPlayed(PlayerState player, CardInstance card, GameState state) { }
}

// Blessing of Grace — all queued cards get +2 speed (permanent)
// Applied in GameManager.HandleQueueCard when checking passives
public class BlessingOfGracePassive : IPassiveEffect
{
    public bool IsInteractive => false;
    public void OnAcquire(PlayerState player, GameState state) { }
    public void OnTurnStart(PlayerState player, GameState state) { }
    public void OnDamageTaken(PlayerState player, int amount, GameState state) { }
    public void OnCardPlayed(PlayerState player, CardInstance card, GameState state) { }
}

// Blessing of Fortitude — gain 3 shield at turn start (permanent)
public class BlessingOfFortitudePassive : IPassiveEffect
{
    public bool IsInteractive => false;
    public void OnAcquire(PlayerState player, GameState state) { }

    public void OnTurnStart(PlayerState player, GameState state)
    {
        player.GainShield(3);
        Debug.Log($"[BlessingOfFortitude] Player {player.PlayerId} gained 3 shield.");
    }

    public void OnDamageTaken(PlayerState player, int amount, GameState state) { }
    public void OnCardPlayed(PlayerState player, CardInstance card, GameState state) { }
}