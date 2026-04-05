using UnityEngine;
using FogClouds;

public class PactOfTheDevilPassive : IPassiveEffect
{
    public bool IsInteractive => false;
    public void OnAcquire(PlayerState player, GameState state) =>
        Debug.Log($"[PactOfTheDevil] Player {player.PlayerId} accepted the pact.");
    public void OnTurnStart(PlayerState player, GameState state)
    {
        player.GainShield(1);
        player.DrawCards(1, state.Rng);
    }
    public void OnDamageTaken(PlayerState player, int amount, GameState state) { }
    public void OnCardPlayed(PlayerState player, CardInstance card, GameState state) { }
    public void OnHPDamaged(PlayerState player, int amount, GameState state) { }
    public void OnPermanentDestroyed(PlayerState player, BoardPermanent permanent, int destroyerPlayerId, GameState state) { }
}

public class BlessingOfValorPassive : IPassiveEffect
{
    public bool IsInteractive => false;
    public void OnAcquire(PlayerState player, GameState state) { }
    public void OnTurnStart(PlayerState player, GameState state) { }
    public void OnDamageTaken(PlayerState player, int amount, GameState state) { }
    public void OnCardPlayed(PlayerState player, CardInstance card, GameState state) { }
    public void OnHPDamaged(PlayerState player, int amount, GameState state) { }
    public void OnPermanentDestroyed(PlayerState player, BoardPermanent permanent, int destroyerPlayerId, GameState state) { }
}

public class BlessingOfClarityPassive : IPassiveEffect
{
    public bool IsInteractive => false;
    public void OnAcquire(PlayerState player, GameState state)
    {
        player.DrawCards(3, state.Rng);
        var passive = player.Passives.Find(p => p.PassiveId == "blessing_of_clarity");
        if (passive != null) passive.StackCount = 1;
    }
    public void OnTurnStart(PlayerState player, GameState state) { }
    public void OnDamageTaken(PlayerState player, int amount, GameState state) { }
    public void OnCardPlayed(PlayerState player, CardInstance card, GameState state) { }
    public void OnHPDamaged(PlayerState player, int amount, GameState state) { }
    public void OnPermanentDestroyed(PlayerState player, BoardPermanent permanent, int destroyerPlayerId, GameState state) { }
}

public class BlessingOfGracePassive : IPassiveEffect
{
    public bool IsInteractive => false;
    public void OnAcquire(PlayerState player, GameState state) { }
    public void OnTurnStart(PlayerState player, GameState state) { }
    public void OnDamageTaken(PlayerState player, int amount, GameState state) { }
    public void OnCardPlayed(PlayerState player, CardInstance card, GameState state) { }
    public void OnHPDamaged(PlayerState player, int amount, GameState state) { }
    public void OnPermanentDestroyed(PlayerState player, BoardPermanent permanent, int destroyerPlayerId, GameState state) { }
}

public class BlessingOfFortitudePassive : IPassiveEffect
{
    public bool IsInteractive => false;
    public void OnAcquire(PlayerState player, GameState state) { }
    public void OnTurnStart(PlayerState player, GameState state) => player.GainShield(3);
    public void OnDamageTaken(PlayerState player, int amount, GameState state) { }
    public void OnCardPlayed(PlayerState player, CardInstance card, GameState state) { }
    public void OnHPDamaged(PlayerState player, int amount, GameState state) { }
    public void OnPermanentDestroyed(PlayerState player, BoardPermanent permanent, int destroyerPlayerId, GameState state) { }
}

// Season of Harvest — +1 per-turn resource permanently
public class SeasonOfHarvestPassive : IPassiveEffect
{
    public bool IsInteractive => false;
    public void OnAcquire(PlayerState player, GameState state)
    {
        player.Resources.BonusPerTurnResource += 1;
        Debug.Log($"[SeasonOfHarvest] Player {player.PlayerId} gains +1 per-turn resource.");
    }
    public void OnTurnStart(PlayerState player, GameState state) { }
    public void OnDamageTaken(PlayerState player, int amount, GameState state) { }
    public void OnCardPlayed(PlayerState player, CardInstance card, GameState state) { }
    public void OnHPDamaged(PlayerState player, int amount, GameState state) { }
    public void OnPermanentDestroyed(PlayerState player, BoardPermanent permanent, int destroyerPlayerId, GameState state) { }
}

// Accelerator — speed logic lives in GameState.EnqueueCard and GameManager.HandleQueueCard
public class AcceleratorPassive : IPassiveEffect
{
    public bool IsInteractive => false;
    public void OnAcquire(PlayerState player, GameState state) { }
    public void OnTurnStart(PlayerState player, GameState state) { }
    public void OnDamageTaken(PlayerState player, int amount, GameState state) { }
    public void OnCardPlayed(PlayerState player, CardInstance card, GameState state) { }
    public void OnHPDamaged(PlayerState player, int amount, GameState state) { }
    public void OnPermanentDestroyed(PlayerState player, BoardPermanent permanent, int destroyerPlayerId, GameState state) { }
}

// Slight of Hand — interactive; logic lives in GameManager.HandleSlightOfHand
public class SlightOfHandPassive : IPassiveEffect
{
    public bool IsInteractive => true;
    public void OnAcquire(PlayerState player, GameState state) { }
    public void OnTurnStart(PlayerState player, GameState state) { }
    public void OnDamageTaken(PlayerState player, int amount, GameState state) { }
    public void OnCardPlayed(PlayerState player, CardInstance card, GameState state) { }
    public void OnHPDamaged(PlayerState player, int amount, GameState state) { }
    public void OnPermanentDestroyed(PlayerState player, BoardPermanent permanent, int destroyerPlayerId, GameState state) { }
}

// Clutterstorm — fires on first HP damage per turn during MainPhase
public class ClutterstormPassive : IPassiveEffect
{
    public bool IsInteractive => false;
    public void OnAcquire(PlayerState player, GameState state) { }
    public void OnTurnStart(PlayerState player, GameState state) { }
    public void OnDamageTaken(PlayerState player, int amount, GameState state) { }
    public void OnCardPlayed(PlayerState player, CardInstance card, GameState state) { }

    public void OnHPDamaged(PlayerState player, int amount, GameState state)
    {
        if (state.CurrentPhase != TurnPhase.MainPhase) return;

        var passive = player.Passives.Find(p => p.PassiveId == "clutterstorm");
        if (passive == null || passive.StackCount > 0) return; // already triggered this turn

        passive.StackCount = 1;

        var clutterstormCard = new CardInstance
        {
            InstanceId = state.GenerateInstanceId(),
            CardId = "clutterstorm_effect",
            DisplayName = "Clutterstorm",
            Type = CardType.Queueable,
            ModifiedSpeed = 0,
            IsAttack = false
        };

        state.EnqueueCard(player.PlayerId, clutterstormCard, wasUpcast: false);
        Debug.Log($"[Clutterstorm] Player {player.PlayerId} triggered Clutterstorm.");
    }

    public void OnPermanentDestroyed(PlayerState player, BoardPermanent permanent, int destroyerPlayerId, GameState state) { }
}

// Unbroken Chain — stack logic lives in GameManager.ApplyUnbrokenChain
public class UnbrokenChainPassive : IPassiveEffect
{
    public bool IsInteractive => false;
    public void OnAcquire(PlayerState player, GameState state) { }
    public void OnTurnStart(PlayerState player, GameState state) { }
    public void OnDamageTaken(PlayerState player, int amount, GameState state) { }
    public void OnCardPlayed(PlayerState player, CardInstance card, GameState state) { }
    public void OnHPDamaged(PlayerState player, int amount, GameState state) { }
    public void OnPermanentDestroyed(PlayerState player, BoardPermanent permanent, int destroyerPlayerId, GameState state) { }
}

// Nature's Eye — logic lives in GameManager.HandleRoguelikeInsight (give 2 Sight instead of 1)
public class NaturesEyePassive : IPassiveEffect
{
    public bool IsInteractive => false;
    public void OnAcquire(PlayerState player, GameState state) { }
    public void OnTurnStart(PlayerState player, GameState state) { }
    public void OnDamageTaken(PlayerState player, int amount, GameState state) { }
    public void OnCardPlayed(PlayerState player, CardInstance card, GameState state) { }
    public void OnHPDamaged(PlayerState player, int amount, GameState state) { }
    public void OnPermanentDestroyed(PlayerState player, BoardPermanent permanent, int destroyerPlayerId, GameState state) { }
}

// Market Crash — interactive; logic lives in GameManager.HandleMarketCrash
public class MarketCrashPassive : IPassiveEffect
{
    public bool IsInteractive => true;
    public void OnAcquire(PlayerState player, GameState state) { }
    public void OnTurnStart(PlayerState player, GameState state) { }
    public void OnDamageTaken(PlayerState player, int amount, GameState state) { }
    public void OnCardPlayed(PlayerState player, CardInstance card, GameState state) { }
    public void OnHPDamaged(PlayerState player, int amount, GameState state) { }
    public void OnPermanentDestroyed(PlayerState player, BoardPermanent permanent, int destroyerPlayerId, GameState state) { }
}

// Blessed Diary — interactive for targeting; reacts to opponent destroying marked permanent
public class BlessedDiaryPassive : IPassiveEffect
{
    public bool IsInteractive => true;
    public void OnAcquire(PlayerState player, GameState state) { }
    public void OnTurnStart(PlayerState player, GameState state) { }
    public void OnDamageTaken(PlayerState player, int amount, GameState state) { }
    public void OnCardPlayed(PlayerState player, CardInstance card, GameState state) { }
    public void OnHPDamaged(PlayerState player, int amount, GameState state) { }

    public void OnPermanentDestroyed(PlayerState player, BoardPermanent permanent, int destroyerPlayerId, GameState state)
    {
        var passive = player.Passives.Find(p => p.PassiveId == "blessed_diary");
        if (passive == null || string.IsNullOrEmpty(passive.TargetPermanentId)) return;
        if (permanent.PermanentId != passive.TargetPermanentId) return;

        player.DrawCards(3, state.Rng);
        passive.TargetPermanentId = null; // diary detaches — can be re-attached
        Debug.Log($"[BlessedDiary] Player {player.PlayerId} drew 3 cards from Blessed Diary trigger.");
    }
}

// Ancient Telescope — interactive; logic lives in GameManager.HandleAncientTelescope
public class AncientTelescopePassive : IPassiveEffect
{
    public bool IsInteractive => true;
    public void OnAcquire(PlayerState player, GameState state) { }
    public void OnTurnStart(PlayerState player, GameState state) { }
    public void OnDamageTaken(PlayerState player, int amount, GameState state) { }
    public void OnCardPlayed(PlayerState player, CardInstance card, GameState state) { }
    public void OnHPDamaged(PlayerState player, int amount, GameState state) { }
    public void OnPermanentDestroyed(PlayerState player, BoardPermanent permanent, int destroyerPlayerId, GameState state) { }
}

// Ceremonial Dagger — applies Bleed whenever owner applies any status effect to opponent
// Logic inline in GameManager.HandleApplyStatusEffect via passive check
public class CeremonialDaggerPassive : IPassiveEffect
{
    public bool IsInteractive => false;
    public void OnAcquire(PlayerState player, GameState state) { }
    public void OnTurnStart(PlayerState player, GameState state) { }
    public void OnDamageTaken(PlayerState player, int amount, GameState state) { }
    public void OnCardPlayed(PlayerState player, CardInstance card, GameState state) { }
    public void OnHPDamaged(PlayerState player, int amount, GameState state) { }
    public void OnPermanentDestroyed(PlayerState player, BoardPermanent permanent, int destroyerPlayerId, GameState state) { }
}

// Cha Cha Lifelong Companion — swaps Cha Cha base card in deck on acquire
public class ChaChaLifelongCompanionPassive : IPassiveEffect
{
    public bool IsInteractive => false;

    public void OnAcquire(PlayerState player, GameState state)
    {
        // Find Cha Cha base card anywhere in deck, hand, or discard and upgrade it
        var allPiles = new System.Collections.Generic.List<System.Collections.Generic.List<CardInstance>>
            { player.Deck, player.Hand, player.Discard };

        foreach (var pile in allPiles)
        {
            var chacha = pile.Find(c => c.CardId == "cha_cha");
            if (chacha != null)
            {
                chacha.CardId = "cha_cha_lifelong";
                chacha.DisplayName = "Cha Cha — Lifelong Companion";
                Debug.Log($"[ChaChaLifelong] Upgraded Cha Cha in Player {player.PlayerId}'s {pile}.");
                return;
            }
        }

        Debug.LogWarning($"[ChaChaLifelong] Player {player.PlayerId} has no Cha Cha card to upgrade.");
    }

    public void OnTurnStart(PlayerState player, GameState state) { }
    public void OnDamageTaken(PlayerState player, int amount, GameState state) { }
    public void OnCardPlayed(PlayerState player, CardInstance card, GameState state) { }
    public void OnHPDamaged(PlayerState player, int amount, GameState state) { }
    public void OnPermanentDestroyed(PlayerState player, BoardPermanent permanent, int destroyerPlayerId, GameState state) { }
}