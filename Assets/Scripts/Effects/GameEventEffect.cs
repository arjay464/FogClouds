using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FogClouds;

// ── The Chopping Block ────────────────────────────────────────────────────────
// Each player submits a comma-separated list of up to 2 instance IDs to remove
public class ChoppingBlockEffect : IGameEventEffect
{
    public bool IsInteractive => true;

    public void Apply(GameState state) { } // nothing to apply globally

    public bool ValidateChoice(GameState state, int playerId, string choice, out string reason)
    {
        reason = null;
        if (string.IsNullOrEmpty(choice)) return true; // removing 0 cards is valid

        var ids = ParseIds(choice);
        if (ids.Count > 2) { reason = "Can only remove up to 2 cards."; return false; }

        var player = state.GetPlayer(playerId);
        foreach (var id in ids)
        {
            if (!player.Deck.Any(c => c.InstanceId == id) &&
                !player.Discard.Any(c => c.InstanceId == id))
            { reason = $"Card {id} not found in deck or discard."; return false; }
        }
        return true;
    }

    public void ApplyChoice(GameState state, int playerId, string choice)
    {
        if (string.IsNullOrEmpty(choice)) return;
        var player = state.GetPlayer(playerId);
        foreach (var id in ParseIds(choice))
        {
            player.Deck.RemoveAll(c => c.InstanceId == id);
            player.Discard.RemoveAll(c => c.InstanceId == id);
            Debug.Log($"[ChoppingBlock] Player {playerId} removed card {id}.");
        }
    }

    public bool IsResolved(GameState state) =>
        state.PlayerEventChoices[0] != null && state.PlayerEventChoices[1] != null;

    private List<int> ParseIds(string choice) =>
        choice.Split(',')
              .Where(s => int.TryParse(s.Trim(), out _))
              .Select(s => int.Parse(s.Trim()))
              .ToList();
}

// ── A Blessing in Disguise ────────────────────────────────────────────────────
// Each player picks a blessing ID for their OPPONENT
// Choice format: "blessing_of_valor" etc.
public class BlessingInDisguiseEffect : IGameEventEffect
{
    private static readonly string[] ValidBlessings = {
        "blessing_of_valor", "blessing_of_clarity",
        "blessing_of_grace", "blessing_of_fortitude"
    };

    public bool IsInteractive => true;
    public void Apply(GameState state) { }

    public bool ValidateChoice(GameState state, int playerId, string choice, out string reason)
    {
        reason = null;
        if (!ValidBlessings.Contains(choice))
        { reason = $"Invalid blessing: {choice}"; return false; }
        return true;
    }

    public void ApplyChoice(GameState state, int playerId, string choice)
    {
        // Player chooses a blessing for their OPPONENT
        int opponentId = playerId == 0 ? 1 : 0;
        var opponent = state.GetPlayer(opponentId);

        var existing = opponent.Passives.Find(p => p.PassiveId == choice);
        if (existing != null)
            existing.StackCount++;
        else
            opponent.Passives.Add(new Passive
            {
                PassiveId = choice,
                DisplayName = FormatName(choice),
                StackCount = 1
            });

        var effect = PassiveRegistry.Instance.GetEffect(choice);
        effect?.OnAcquire(opponent, state);
        Debug.Log($"[BlessingInDisguise] Player {playerId} gave {choice} to Player {opponentId}.");
    }

    public bool IsResolved(GameState state) =>
        state.PlayerEventChoices[0] != null && state.PlayerEventChoices[1] != null;

    private string FormatName(string id) =>
        string.Concat(id.Split('_').Select(p => char.ToUpper(p[0]) + p.Substring(1) + " ")).Trim();
}

// ── The Calm Before the Storm ─────────────────────────────────────────────────
// Non-interactive — heals both players 30% HP, applies "Blessed by the Storm" status
public class CalmBeforeTheStormEffect : IGameEventEffect
{
    public bool IsInteractive => false;

    public void Apply(GameState state)
    {
        foreach (var player in new[] { state.GetPlayer(0), state.GetPlayer(1) })
        {
            int heal = Mathf.CeilToInt(player.Character.BaseHP * 0.3f);
            player.HP = Mathf.Min(player.HP + heal, player.Character.BaseHP);
            player.ApplyStatusEffect(new StatusEffect("blessed_by_storm", value: 2, duration: 1));
            Debug.Log($"[CalmBeforeStorm] Player {player.PlayerId} healed {heal} HP, got Blessed by the Storm.");
        }
    }

    public bool ValidateChoice(GameState state, int playerId, string choice, out string reason)
    { reason = null; return true; }
    public void ApplyChoice(GameState state, int playerId, string choice) { }
    public bool IsResolved(GameState state) => true;
}

// ── Too Good to be True ───────────────────────────────────────────────────────
// Each player picks "silver" or "sight"
public class TooGoodToBeTrueEffect : IGameEventEffect
{
    public bool IsInteractive => true;
    public void Apply(GameState state) { }

    public bool ValidateChoice(GameState state, int playerId, string choice, out string reason)
    {
        reason = null;
        if (choice != "silver" && choice != "sight")
        { reason = "Choice must be 'silver' or 'sight'."; return false; }
        return true;
    }

    public void ApplyChoice(GameState state, int playerId, string choice)
    {
        var player = state.GetPlayer(playerId);
        if (choice == "silver")
        {
            player.Silver += 10;
            Debug.Log($"[TooGoodToBeTrue] Player {playerId} gained 10 Silver.");
        }
        else
        {
            player.InsightTree.SightBanked += 2;
            Debug.Log($"[TooGoodToBeTrue] Player {playerId} gained 2 Sight.");
        }
    }

    public bool IsResolved(GameState state) =>
        state.PlayerEventChoices[0] != null && state.PlayerEventChoices[1] != null;
}

// ── A Deal with the Devil ─────────────────────────────────────────────────────
// Each player picks "deal" or "pass"
public class DealWithTheDevilEffect : IGameEventEffect
{
    public bool IsInteractive => true;
    public void Apply(GameState state) { }

    public bool ValidateChoice(GameState state, int playerId, string choice, out string reason)
    {
        reason = null;
        if (choice != "deal" && choice != "pass")
        { reason = "Choice must be 'deal' or 'pass'."; return false; }
        return true;
    }

    public void ApplyChoice(GameState state, int playerId, string choice)
    {
        if (choice != "deal") return;
        var player = state.GetPlayer(playerId);

        // Lose half HP rounded unfavorably (ceiling), minimum 1
        int loss = Mathf.CeilToInt(player.HP / 2f);
        player.HP = Mathf.Max(1, player.HP - loss);

        // Grant Pact of the Devil passive
        var existing = player.Passives.Find(p => p.PassiveId == "pact_of_the_devil");
        if (existing != null)
            existing.StackCount++;
        else
            player.Passives.Add(new Passive
            {
                PassiveId = "pact_of_the_devil",
                DisplayName = "Pact of the Devil",
                StackCount = 1
            });

        var effect = PassiveRegistry.Instance.GetEffect("pact_of_the_devil");
        effect?.OnAcquire(player, state);
        Debug.Log($"[DealWithTheDevil] Player {playerId} took the deal. Lost {loss} HP, gained Pact of the Devil.");
    }

    public bool IsResolved(GameState state) =>
        state.PlayerEventChoices[0] != null && state.PlayerEventChoices[1] != null;
}

// ── The Writing on the Wall ───────────────────────────────────────────────────
// Server reveals top 5 cards to each player. Choice is comma-separated instance IDs to discard.
public class WritingOnTheWallEffect : IGameEventEffect
{
    public bool IsInteractive => true;

    public void Apply(GameState state)
    {
        // Reveal top 5 cards to each player, reshuffling discard under deck if needed
        state.EventRevealedCards = new List<CardInstance>[2];
        for (int i = 0; i < 2; i++)
        {
            var player = state.GetPlayer(i);
            state.EventRevealedCards[i] = new List<CardInstance>();

            // If deck has fewer than 5, shuffle discard UNDER remaining deck cards
            if (player.Deck.Count < 5 && player.Discard.Count > 0)
            {
                var shuffled = new List<CardInstance>(player.Discard);
                player.Discard.Clear();
                // Fisher-Yates shuffle
                for (int j = shuffled.Count - 1; j > 0; j--)
                {
                    int k = state.Rng.Next(j + 1);
                    var tmp = shuffled[j]; shuffled[j] = shuffled[k]; shuffled[k] = tmp;
                }
                // Existing deck cards stay at top, shuffled discard goes underneath
                player.Deck.AddRange(shuffled);
            }

            // Reveal top 5 (or all if fewer)
            int count = Mathf.Min(5, player.Deck.Count);
            state.EventRevealedCards[i].AddRange(player.Deck.Take(count));
        }
    }

    public bool ValidateChoice(GameState state, int playerId, string choice, out string reason)
    {
        reason = null;
        if (string.IsNullOrEmpty(choice)) return true;

        var ids = ParseIds(choice);
        var revealed = state.EventRevealedCards?[playerId];
        if (revealed == null) { reason = "No revealed cards found."; return false; }

        foreach (var id in ids)
            if (!revealed.Any(c => c.InstanceId == id))
            { reason = $"Card {id} was not in the revealed set."; return false; }

        return true;
    }

    public void ApplyChoice(GameState state, int playerId, string choice)
    {
        var player = state.GetPlayer(playerId);
        if (string.IsNullOrEmpty(choice)) return;

        foreach (var id in ParseIds(choice))
        {
            player.Deck.RemoveAll(c => c.InstanceId == id);
            Debug.Log($"[WritingOnTheWall] Player {playerId} discarded card {id} from deck.");
        }
    }

    public bool IsResolved(GameState state) =>
        state.PlayerEventChoices[0] != null && state.PlayerEventChoices[1] != null;

    private List<int> ParseIds(string choice) =>
        choice.Split(',')
              .Where(s => int.TryParse(s.Trim(), out _))
              .Select(s => int.Parse(s.Trim()))
              .ToList();
}

// ── Fortune Favors the Bold ───────────────────────────────────────────────────
// Each player picks a bet: "5", "10", "20", or "50" HP
// Server resolves with 50/50 RNG — win heals that much, lose costs that much
public class FortuneFavorsTheBoldEffect : IGameEventEffect
{
    public bool IsInteractive => true;
    public void Apply(GameState state) { }

    public bool ValidateChoice(GameState state, int playerId, string choice, out string reason)
    {
        reason = null;
        var valid = new[] { "5", "10", "20", "50" };
        if (!valid.Contains(choice)) { reason = "Bet must be 5, 10, 20, or 50."; return false; }

        int bet = int.Parse(choice);
        var player = state.GetPlayer(playerId);
        // Minimum bet is 5 — always must gamble regardless of HP
        // (if below 5 HP, the minimum 5 bet applies and they risk dying)
        return true;
    }

    public void ApplyChoice(GameState state, int playerId, string choice)
    {
        int bet = int.Parse(choice);
        var player = state.GetPlayer(playerId);

        // 50/50 coin flip
        bool win = state.Rng.Next(2) == 0;
        if (win)
        {
            int heal = Mathf.Min(bet, player.Character.BaseHP - player.HP);
            player.HP += heal;
            Debug.Log($"[FortuneFavorsTheBold] Player {playerId} WON bet of {bet} — healed {heal} HP.");
        }
        else
        {
            player.HP = Mathf.Max(0, player.HP - bet);
            Debug.Log($"[FortuneFavorsTheBold] Player {playerId} LOST bet of {bet}.");
            state.CheckWinCondition();
        }
    }

    public bool IsResolved(GameState state) =>
        state.PlayerEventChoices[0] != null && state.PlayerEventChoices[1] != null;
}