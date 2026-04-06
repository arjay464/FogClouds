using System;
using UnityEngine;
using System.Linq;
using System.ComponentModel;
using System.Collections.Generic;

namespace FogClouds
{

    //Targeting effects must implement ApplyTargeted
    public interface ITargetedEffect
    {
        void ApplyTargeted(QueueEntry source, GameState state, int targetInstanceId);
    }


    // DealDamage — applies damage through the full pipeline:
    // attacker board buffs → defender board reductions → shield → HP → lifesteal
    public class DealDamageEffect : ICardEffect
    {
        public int Amount;
        public bool Lifesteal;

        public DealDamageEffect(int amount, bool lifesteal = false)
        {
            Amount = amount;
            Lifesteal = lifesteal;
        }

        public void Apply(QueueEntry source, GameState state)
        {
            PlayerState attacker = state.GetPlayer(source.OwnerId);
            PlayerState defender = state.GetOpponent(source.OwnerId);

            int damage = Amount + source.BonusDamage;

            // Blessed by the Storm
            var stormBuff = attacker.StatusEffects.Find(e => e.EffectId == "blessed_by_storm");
            if (stormBuff != null && source.Card.IsAttack)
                damage += stormBuff.Value;

            // Attacker board buffs
            foreach (var permanent in attacker.Board)
                if (permanent is IDamageModifier mod)
                    damage = mod.ModifyDamage(damage, isAttacker: true);

            // Defender board reductions
            foreach (var permanent in defender.Board)
                if (permanent is IDamageModifier mod)
                    damage = mod.ModifyDamage(damage, isAttacker: false);

            // Brand of Fragility — reduce damage of next damage card
            if (state.NextDamageReduction > 0)
            {
                damage = Mathf.Max(0, damage - state.NextDamageReduction);
                state.NextDamageReduction = 0;
            }

            // Counterstrike — convert attack damage to shield for defender instead
            if (defender.ParryActive && source.Card.IsAttack)
            {
                defender.GainShield(Amount); // base amount only, no buffs
                defender.ParryActive = false;
                Debug.Log($"[DealDamageEffect] Counterstrike absorbed {Amount} damage as shield.");
                return;
            }

            int preShieldDamage = damage;
            int shieldBefore = defender.Shield;
            defender.TakeDamage(damage);
            int actualDamage = Math.Max(0, preShieldDamage - shieldBefore);

            if (actualDamage > 0)
            {
                attacker.Silver += actualDamage;

                // OnHPDamaged passive hook
                foreach (var passive in defender.Passives)
                {
                    var effect = PassiveRegistry.Instance.GetEffect(passive.PassiveId);
                    effect?.OnHPDamaged(defender, actualDamage, state);
                }

                // Totem of Warding — apply bleed to attacker
                foreach (var permanent in defender.Board)
                    if (permanent is IDamageTakenReactor reactor)
                        reactor.OnDamageTakenFromOpponent(defender, attacker, actualDamage, state);
            }

            if (Lifesteal)
            {
                int heal = Math.Min(actualDamage, attacker.Character.BaseHP - attacker.HP);
                attacker.HP += heal;
                Debug.Log($"[DealDamageEffect] Lifesteal healed {heal} HP.");
            }

            state.CheckWinCondition();
            Debug.Log($"[DealDamageEffect] Player {source.OwnerId} dealt {actualDamage} damage to opponent.");
        }
    }

    // GainShield — adds shield to the casting player
    public class GainShieldEffect : ICardEffect
    {
        public int Amount;

        public GainShieldEffect(int amount)
        {
            Amount = amount;
        }

        public void Apply(QueueEntry source, GameState state)
        {
            PlayerState player = state.GetPlayer(source.OwnerId);
            player.GainShield(Amount);
            Debug.Log($"[GainShieldEffect] Player {source.OwnerId} gained {Amount} shield.");
        }
    }

    // HealEffect — restores HP up to the character's base HP cap. No overheal.
    public class HealEffect : ICardEffect
    {
        public int Amount;

        public HealEffect(int amount)
        {
            Amount = amount;
        }

        public void Apply(QueueEntry source, GameState state)
        {
            PlayerState player = state.GetPlayer(source.OwnerId);
            int heal = Mathf.Min(Amount, player.Character.BaseHP - player.HP);
            player.HP += heal;
            Debug.Log($"[HealEffect] Player {source.OwnerId} healed {heal} HP.");
        }
    }

    // AddDaggerEffect — adds one Dagger to the casting player's per-turn resource
    public class AddDaggerEffect : ICardEffect
    {
        public void Apply(QueueEntry source, GameState state)
        {
            PlayerState player = state.GetPlayer(source.OwnerId);
            player.Resources.PerTurnResource += 1;
            Debug.Log($"[AddDaggerEffect] Player {source.OwnerId} gained 1 Dagger. Total: {player.Resources.PerTurnResource}");
        }
    }

    // SpawnPermanentEffect — places a BoardPermanent on the casting player's board
    public class SpawnPermanentEffect : ICardEffect
    {
        public string PermanentId;
        public string DisplayName;
        public int TurnsRemaining;

        public SpawnPermanentEffect(string permanentId, string displayName, int turnsRemaining)
        {
            PermanentId = permanentId;
            DisplayName = displayName;
            TurnsRemaining = turnsRemaining;
        }

        public void Apply(QueueEntry source, GameState state)
        {
            PlayerState player = state.GetPlayer(source.OwnerId);
            BoardPermanent permanent = CreatePermanent(source.OwnerId);
            permanent.SourceCard = source.Card;
            state.AssignPermanentInstanceId(permanent);
            player.Board.Add(permanent);
            Debug.Log($"[SpawnPermanentEffect] Player {source.OwnerId} spawned {DisplayName}.");
        }

        private BoardPermanent CreatePermanent(int ownerId)
        {
            switch (PermanentId)
            {
                case "cha_cha":
                    return new ChaCha(ownerId, bonusDamage: 1);
                case "cursed_goblet":
                    return new CursedGoblet(ownerId, reductionPercent: 0.2f);
                case "totem_of_warding":
                    return new TotemOfWarding(ownerId);
                default:
                    Debug.LogWarning($"[SpawnPermanentEffect] No subclass found for {PermanentId}, spawning base BoardPermanent.");
                    return new BoardPermanent
                    {
                        PermanentId = PermanentId,
                        DisplayName = DisplayName,
                        OwnerId = ownerId,
                        TurnsRemaining = TurnsRemaining
                    };
            }
        }
    }

    // MultiHitEffect — applies DealDamage multiple times sequentially (e.g. Twin Slash)
    public class MultiHitEffect : ICardEffect
    {
        public int HitCount;
        public int DamagePerHit;
        public bool Lifesteal;

        public MultiHitEffect(int hitCount, int damagePerHit, bool lifesteal = false)
        {
            HitCount = hitCount;
            DamagePerHit = damagePerHit;
            Lifesteal = lifesteal;
        }

        public void Apply(QueueEntry source, GameState state)
        {
            var singleHit = new DealDamageEffect(DamagePerHit, Lifesteal);
            for (int i = 0; i < HitCount; i++)
            {
                singleHit.Apply(source, state);
                if (state.GameOver) return;
            }
        }
    }
    // BloodHexEffect — adds 2 useless Blood Hex cards to opponent's draw pile
    public class BloodHexEffect : ICardEffect
    {
        public void Apply(QueueEntry source, GameState state)
        {
            var opponent = state.GetOpponent(source.OwnerId);
            var def = Resources.Load<CardDefinition>("Cards/BloodHexCurse");
            if (def == null)
            {
                Debug.LogWarning("[BloodHexEffect] Could not load BloodHexCurse CardDefinition.");
                return;
            }
            for (int i = 0; i < 2; i++)
            {
                var card = new CardInstance(def, state.GenerateInstanceId());
                // Insert at a random position in the draw pile
                int insertAt = state.Rng.Next(opponent.Deck.Count + 1);
                opponent.Deck.Insert(insertAt, card);
            }
            Debug.Log($"[BloodHexEffect] Added 2 Blood Hex curses to opponent's deck.");
        }
    }

    // HemorrhageEffect — deals damage equal to blood spent this turn by the caster
    public class HemorrhageEffect : ICardEffect
    {
        public bool Lifesteal;

        public HemorrhageEffect(bool lifesteal = false)
        {
            Lifesteal = lifesteal;
        }

        public void Apply(QueueEntry source, GameState state)
        {
            var attacker = state.GetPlayer(source.OwnerId);
            int damage = attacker.BloodSpentThisTurn;
            if (damage <= 0)
            {
                Debug.Log($"[HemorrhageEffect] No blood spent this turn — 0 damage.");
                return;
            }
            var singleHit = new DealDamageEffect(damage, lifesteal: Lifesteal);
            singleHit.Apply(source, state);
            Debug.Log($"[HemorrhageEffect] Dealt {damage} damage based on blood spent.");
        }
    }

    // BloodrushEffect — deals 4 damage, or 8 if caster is below half HP
    public class BloodrushEffect : ICardEffect
    {

        public bool Lifesteal;

        public BloodrushEffect(bool lifesteal = false)
        {
            lifesteal = Lifesteal;
        }

        public void Apply(QueueEntry source, GameState state)
        {
            var attacker = state.GetPlayer(source.OwnerId);
            bool belowHalf = attacker.HP < attacker.Character.BaseHP / 2f;
            int damage = belowHalf ? 8 : 4;
            var hit = new DealDamageEffect(damage, lifesteal: Lifesteal);
            hit.Apply(source, state);
            Debug.Log($"[BloodrushEffect] Dealt {damage} damage (below half: {belowHalf}).");
        }
    }

    // ArterialCutEffect — applies Bleed to the opponent (1 damage at TurnStart)
    public class ArterialCutEffect : ICardEffect
    {
        public void Apply(QueueEntry source, GameState state)
        {
            var opponent = state.GetOpponent(source.OwnerId);
            // Bleed stacks — add a new status effect each application
            opponent.ApplyStatusEffect(new StatusEffect("bleed", value: 1, duration: -1));
            Debug.Log($"[ArterialCutEffect] Applied Bleed to opponent.");
        }
    }

    // HiddenDaggersEffect — instant: queues 2 Thrown Daggers into personal queue at speed 10
    public class HiddenDaggersEffect : ICardEffect
    {
        public void Apply(QueueEntry source, GameState state)
        {
            var def = Resources.Load<CardDefinition>("Cards/ThrownDagger");
            if (def == null)
            {
                Debug.LogWarning("[HiddenDaggersEffect] Could not load ThrownDagger CardDefinition.");
                return;
            }
            var queue = state.GetQueue(source.OwnerId);
            for (int i = 0; i < 2; i++)
            {
                var dagger = new CardInstance(def, state.GenerateInstanceId());
                queue.Add(new QueueEntry(source.OwnerId, dagger, dagger.BaseSpeed));
            }
            Debug.Log($"[HiddenDaggersEffect] Queued 2 Thrown Daggers for Player {source.OwnerId}.");
        }
    }

    // TotemOfSharpnessEffect — spawns TotemOfSharpness permanent (2 turns)
    // The permanent itself is defined in PermanentDefinitions.cs
    public class TotemOfSharpnessEffect : ICardEffect
    {
        public void Apply(QueueEntry source, GameState state)
        {
            var player = state.GetPlayer(source.OwnerId);
            var totem = new TotemOfSharpness(source.OwnerId, bonusDamage: 3, turnsRemaining: 2) { SourceCard = source.Card };
            state.AssignPermanentInstanceId(totem);
            player.Board.Add(totem);
            Debug.Log($"[TotemOfSharpnessEffect] Player {source.OwnerId} spawned Totem of Sharpness.");
        }
    }

    // DaggersInTheDarkEffect — gains 5 shield per dagger spent this turn, costs 1 blood
    public class DaggersInTheDarkEffect : ICardEffect
    {
        public void Apply(QueueEntry source, GameState state)
        {
            var player = state.GetPlayer(source.OwnerId);
            int shield = 5 * player.DaggersSpentThisTurn;
            player.GainShield(shield);
            Debug.Log($"[DaggersInTheDarkEffect] Player {source.OwnerId} gained {shield} shield ({player.DaggersSpentThisTurn} daggers spent).");
        }
    }

    // ThroughBloodshedEffect — draw 3 cards
    public class ThroughBloodshedEffect : ICardEffect
    {
        public void Apply(QueueEntry source, GameState state)
        {
            var player = state.GetPlayer(source.OwnerId);
            player.DrawCards(3, state.Rng);
            Debug.Log($"[ThroughBloodshedEffect] Player {source.OwnerId} drew 3 cards.");
        }
    }

    // MulticultaEffect — grants 2 extra daggers at the start of next turn
    // Uses a status effect with a 1-turn duration; OnTurnStart checks for it
    public class MulticultaEffect : ICardEffect
    {
        public void Apply(QueueEntry source, GameState state)
        {
            var player = state.GetPlayer(source.OwnerId);
            player.ApplyStatusEffect(new StatusEffect("multiculta_daggers", value: 2, duration: 1));
            Debug.Log($"[MulticultaEffect] Player {source.OwnerId} will gain 2 extra Daggers next turn.");
        }
    }

    // SanguinePactEffect — permanently increases max daggers per turn by 1
    public class SanguinePactEffect : ICardEffect
    {
        public void Apply(QueueEntry source, GameState state)
        {
            var player = state.GetPlayer(source.OwnerId);
            player.Resources.PerTurnResourceMax += 1;
            Debug.Log($"[SanguinePactEffect] Player {source.OwnerId} increased max Daggers to {player.Resources.PerTurnResourceMax}.");
        }
    }

    // RecoverEffect — heal 4 HP (instant, no cost)
    public class RecoverEffect : ICardEffect
    {
        public void Apply(QueueEntry source, GameState state)
        {
            var player = state.GetPlayer(source.OwnerId);
            int heal = Mathf.Min(4, player.Character.BaseHP - player.HP);
            player.HP += heal;
            Debug.Log($"[RecoverEffect] Player {source.OwnerId} healed {heal} HP.");
        }
    }

    // MirrorOfMoonlightEffect — activates mirror for 2 turns (copies instants into queue at speed 2)
    public class MirrorOfMoonlightEffect : ICardEffect
    {
        public void Apply(QueueEntry source, GameState state)
        {
            var player = state.GetPlayer(source.OwnerId);
            var mirror = new MirrorOfMoonlight(source.OwnerId, turnsRemaining: 2) { SourceCard = source.Card };
            state.AssignPermanentInstanceId(mirror);
            player.Board.Add(mirror);
            player.MirrorActive = true;
            player.MirrorTurnsRemaining = 2;
            Debug.Log($"[MirrorOfMoonlightEffect] Player {source.OwnerId} activated Mirror of Moonlight for 2 turns.");
        }
    }

    // TotemOfSacrificeEffect — spawns TotemOfSacrifice permanent (infinite, reduces blood costs by 1)
    public class TotemOfSacrificeEffect : ICardEffect
    {
        public void Apply(QueueEntry source, GameState state)
        {
            var player = state.GetPlayer(source.OwnerId);
            var totem = new TotemOfSacrifice(source.OwnerId) { SourceCard = source.Card };
            state.AssignPermanentInstanceId(totem);
            player.Board.Add(totem);
            Debug.Log($"[TotemOfSacrificeEffect] Player {source.OwnerId} spawned Totem of Sacrifice.");
        }
    }

    // FerricidiumEffect — instant: destroys target permanent on opponent's board
    // For alpha, destroys the first permanent found. Targeting UI comes later.
    public class FerricidiumEffect : ICardEffect, ITargetedEffect
    {
        public void Apply(QueueEntry source, GameState state)
        {
            // No target — still valid, do nothing
            Debug.Log("[FerricidiumEffect] No target provided — no effect.");
        }

        public void ApplyTargeted(QueueEntry source, GameState state, int targetInstanceId)
        {
            var opponent = state.GetOpponent(source.OwnerId);
            var target = opponent.Board.Find(p => p.InstanceId == targetInstanceId);
            if (target == null)
            {
                Debug.LogWarning($"[FerricidiumEffect] Target {targetInstanceId} not found.");
                return;
            }
            if (target.ProtectedThisTurn)
            {
                Debug.Log("[FerricidiumEffect] Target is protected.");
                return;
            }
            state.DestroyPermanent(source.OwnerId, target);
        }
    }

    // LexNoctisEffect — instant: protects target permanent this turn
    // Sets LexNoctisProtected flag on the first permanent found. Targeting UI comes later.
    public class LexNoctisEffect : ICardEffect, ITargetedEffect
    {
        public void Apply(QueueEntry source, GameState state)
        {
            Debug.Log("[LexNoctisEffect] No target provided — no effect.");
        }

        public void ApplyTargeted(QueueEntry source, GameState state, int targetInstanceId)
        {
            var player = state.GetPlayer(source.OwnerId);
            var opponent = state.GetOpponent(source.OwnerId);
            Debug.Log($"[Ferricidium] Looking for target {targetInstanceId}. Board IDs: {string.Join(", ", opponent.Board.Select(p => p.InstanceId))}");
            var target = player.Board.Find(p => p.InstanceId == targetInstanceId);
            if (target == null)
            {
                Debug.LogWarning($"[LexNoctisEffect] Target {targetInstanceId} not found.");
                return;
            }
            target.ProtectedThisTurn = true;
        }
    }

    // BanishmentEffect — instant: exiles up to 3 cards from draw pile, returns them in 2 turns
    public class BanishmentEffect : ICardEffect
    {
        public void Apply(QueueEntry source, GameState state)
        {
            var player = state.GetPlayer(source.OwnerId);
            int count = Mathf.Min(3, player.Deck.Count);
            for (int i = 0; i < count; i++)
            {
                // Always exile from top of deck
                var card = player.Deck[0];
                player.Deck.RemoveAt(0);
                player.ExiledCards.Add(new ExiledCard(card, turnsRemaining: 2));
            }
            Debug.Log($"[BanishmentEffect] Player {source.OwnerId} exiled {count} cards for 2 turns.");
        }
    }

    // TotemOfProgressEffect — spawns TotemOfProgress permanent (3 turns, upcast cards draw 1)
    public class TotemOfProgressEffect : ICardEffect
    {
        public void Apply(QueueEntry source, GameState state)
        {
            var player = state.GetPlayer(source.OwnerId);
            var totem = new TotemOfProgress(source.OwnerId, turnsRemaining: 3) { SourceCard = source.Card };
            state.AssignPermanentInstanceId(totem);
            player.Board.Add(totem);
            Debug.Log($"[TotemOfProgressEffect] Player {source.OwnerId} spawned Totem of Progress.");
        }
    }

    // ThrownDaggerEffect — 2 damage with lifesteal (queued by HiddenDaggers)
    public class ThrownDaggerEffect : ICardEffect
    {
        public void Apply(QueueEntry source, GameState state)
        {
            var hit = new DealDamageEffect(2, lifesteal: true);
            hit.Apply(source, state);
        }
    }

    // NoOpEffect — does nothing. Used for junk cards like BloodHexCurse.
    public class NoOpEffect : ICardEffect
    {
        public void Apply(QueueEntry source, GameState state)
        {
            Debug.Log($"[NoOpEffect] {source.Card.DisplayName} resolved with no effect.");
        }
    }

    // SanguimortisEffect — deals 3x opponent's bleed stacks, does not consume bleed
    public class SanguimortisEffect : ICardEffect
    {
        public bool Lifesteal;
        public SanguimortisEffect(bool lifesteal = false) { Lifesteal = lifesteal; }

        public void Apply(QueueEntry source, GameState state)
        {
            var opponent = state.GetOpponent(source.OwnerId);
            var bleed = opponent.StatusEffects.Find(e => e.EffectId == "bleed");
            int damage = bleed != null ? bleed.Value * 3 : 0;
            if (damage <= 0)
            {
                Debug.Log("[SanguimortisEffect] Opponent has no bleed — 0 damage.");
                return;
            }
            new DealDamageEffect(damage, Lifesteal).Apply(source, state);
            Debug.Log($"[SanguimortisEffect] Dealt {damage} damage from {bleed.Value} bleed stacks.");
        }
    }

    // FrenzyEffect — deals 1 damage for each card in merged queue when this resolves (sees itself)
    public class FrenzyEffect : ICardEffect
    {
        public bool Lifesteal;

        public FrenzyEffect(bool lifesteal = false) { Lifesteal = lifesteal; }

        public void Apply(QueueEntry source, GameState state)
        {
            // MergedQueue still contains this entry's siblings; +1 for self
            int cardCount = state.MergedQueue.Count + 1;
            int damage = cardCount;
            new DealDamageEffect(damage, Lifesteal).Apply(source, state);
            Debug.Log($"[FrenzyEffect] {cardCount} cards in queue — dealt {damage} damage.");
        }
    }

    // VulniferaEffect — deal 3 damage, apply 2 bleed
    public class VulniferaEffect : ICardEffect
    {
        public bool Lifesteal;

        public VulniferaEffect(bool lifesteal = false) { Lifesteal = lifesteal; }

        public void Apply(QueueEntry source, GameState state)
        {
            new DealDamageEffect(3, Lifesteal).Apply(source, state);
            if (state.GameOver) return;
            var opponent = state.GetOpponent(source.OwnerId);
            opponent.ApplyStatusEffect(new StatusEffect("bleed", value: 2, duration: -1));
            Debug.Log($"[VulniferaEffect] Dealt 3 damage and applied 2 Bleed.");
        }
    }

    // BloodthirstEffect — deal damage and gain HP equal to opponent's bleed stacks, then remove bleed
    public class BloodthirstEffect : ICardEffect
    {
        public bool Lifesteal;

        public BloodthirstEffect(bool lifesteal = false) { Lifesteal = lifesteal; }

        public void Apply(QueueEntry source, GameState state)
        {
            var attacker = state.GetPlayer(source.OwnerId);
            var opponent = state.GetOpponent(source.OwnerId);
            var bleed = opponent.StatusEffects.Find(e => e.EffectId == "bleed");
            int amount = bleed != null ? bleed.Value : 0;
            if (amount <= 0)
            {
                Debug.Log("[BloodthirstEffect] No bleed to consume.");
                return;
            }
            new DealDamageEffect(amount, Lifesteal).Apply(source, state);
            if (state.GameOver) return;
            int heal = Mathf.Min(amount, attacker.Character.BaseHP - attacker.HP);
            attacker.HP += heal;
            // Remove bleed entirely
            opponent.StatusEffects.RemoveAll(e => e.EffectId == "bleed");
            Debug.Log($"[BloodthirstEffect] Dealt {amount} damage, healed {heal} HP, removed bleed.");
        }
    }

    // WeakpointStrikeEffect — deal 5 damage, or 10 if opponent has no permanents
    public class WeakpointStrikeEffect : ICardEffect
    {
        public bool Lifesteal;

        public WeakpointStrikeEffect(bool lifesteal = false) { Lifesteal = lifesteal; }

        public void Apply(QueueEntry source, GameState state)
        {
            var opponent = state.GetOpponent(source.OwnerId);
            int damage = opponent.Board.Count == 0 ? 10 : 5;
            new DealDamageEffect(damage, Lifesteal).Apply(source, state);
            Debug.Log($"[WeakpointStrikeEffect] Dealt {damage} damage (no permanents: {opponent.Board.Count == 0}).");
        }
    }

    // ShadowstepEffect — deal 2 damage; speed set to fastest in own queue + 2 at queue time (handled in HandleQueueCard)
    public class ShadowstepEffect : ICardEffect
    {
        public bool Lifesteal;

        public ShadowstepEffect(bool lifesteal = false) { Lifesteal = lifesteal; }

        public void Apply(QueueEntry source, GameState state)
        {
            new DealDamageEffect(2, Lifesteal).Apply(source, state);
        }
    }

    // BrandOfFragilityEffect — deal 3 damage, set NextDamageReduction = 3
    public class BrandOfFragilityEffect : ICardEffect
    {
        public bool Lifesteal;

        public BrandOfFragilityEffect(bool lifesteal = false) { Lifesteal = lifesteal; }

        public void Apply(QueueEntry source, GameState state)
        {
            new DealDamageEffect(3, Lifesteal).Apply(source, state);
            if (state.GameOver) return;
            state.NextDamageReduction = 3;
            Debug.Log("[BrandOfFragilityEffect] Next damage card deals 3 less.");
        }
    }

    // InanivoreEffect — deal 5 damage, or 10 if opponent has 0 per-turn resource
    public class InanivoraEffect : ICardEffect
    {
        public bool Lifesteal;

        public InanivoraEffect(bool lifesteal = false) { Lifesteal = lifesteal; }

        public void Apply(QueueEntry source, GameState state)
        {
            var opponent = state.GetOpponent(source.OwnerId);
            int damage = opponent.Resources.PerTurnResource == 0 ? 10 : 5;
            new DealDamageEffect(damage, Lifesteal).Apply(source, state);
            Debug.Log($"[InanivoreEffect] Dealt {damage} damage (opponent daggers: {opponent.Resources.PerTurnResource}).");
        }
    }

    // DeathsCallEffect — if opponent < 20 HP deal 20, otherwise take 20 yourself
    public class DeathsCallEffect : ICardEffect
    {
        public bool Lifesteal;

        public DeathsCallEffect(bool lifesteal = false) { Lifesteal = lifesteal; }

        public void Apply(QueueEntry source, GameState state)
        {
            var attacker = state.GetPlayer(source.OwnerId);
            var opponent = state.GetOpponent(source.OwnerId);
            if (opponent.HP < 20)
            {
                new DealDamageEffect(20).Apply(source, state);
                Debug.Log("[DeathsCallEffect] Opponent below 20 HP — dealt 20 damage.");
            }
            else
            {
                attacker.TakeDamage(20);
                state.CheckWinCondition();
                Debug.Log("[DeathsCallEffect] Opponent above 20 HP — took 20 self damage.");
            }
        }
    }

    // ExsanguinateEffect — deal 20 damage if opponent has < 2 cards in merged queue, else 5
    public class ExsanguinateEffect : ICardEffect
    {
        public bool Lifesteal;

        public ExsanguinateEffect(bool lifesteal = false) { Lifesteal = lifesteal; }

        public void Apply(QueueEntry source, GameState state)
        {
            int opponentCards = state.MergedQueue.Count(e => e.OwnerId != source.OwnerId);
            int damage = opponentCards < 2 ? 20 : 5;
            new DealDamageEffect(damage).Apply(source, state);
            Debug.Log($"[ExsanguinateEffect] Opponent queued {opponentCards} cards — dealt {damage} damage.");
        }
    }

    // LacerateEffect — apply bleed equal to blood spent this turn
    public class LacerateEffect : ICardEffect
    {
        public void Apply(QueueEntry source, GameState state)
        {
            var attacker = state.GetPlayer(source.OwnerId);
            int stacks = attacker.BloodSpentThisTurn;
            if (stacks <= 0)
            {
                Debug.Log("[LacerateEffect] No blood spent — 0 bleed applied.");
                return;
            }
            var opponent = state.GetOpponent(source.OwnerId);
            opponent.ApplyStatusEffect(new StatusEffect("bleed", value: stacks, duration: -1));
            Debug.Log($"[LacerateEffect] Applied {stacks} Bleed from blood spent this turn.");
        }
    }

    // ParryEffect — set ParryActive; scan queue for next opponent attack and pre-add shield
    public class ParryEffect : ICardEffect
    {
        public void Apply(QueueEntry source, GameState state)
        {
            var player = state.GetPlayer(source.OwnerId);
            player.ParryActive = true;
            Debug.Log($"[ParryEffect] Player {source.OwnerId} Parry active.");
        }
    }

    // BloodMirrorEffect — copy the last resolved effect
    public class BloodMirrorEffect : ICardEffect
    {
        public void Apply(QueueEntry source, GameState state)
        {
            if (string.IsNullOrEmpty(state.LastResolvedEffectId))
            {
                Debug.Log("[BloodMirrorEffect] No previous effect to mirror.");
                return;
            }
            var effect = CardEffectRegistry.Instance.GetEffect(state.LastResolvedEffectId);
            if (effect == null)
            {
                Debug.Log($"[BloodMirrorEffect] Effect {state.LastResolvedEffectId} not found.");
                return;
            }
            effect.Apply(source, state);
            Debug.Log($"[BloodMirrorEffect] Mirrored effect: {state.LastResolvedEffectId}.");
        }
    }

    // AcceleratedCutEffect — apply 1 bleed, grant +5 speed to next card queued
    public class AcceleratedCutEffect : ICardEffect
    {
        public void Apply(QueueEntry source, GameState state)
        {
            var opponent = state.GetOpponent(source.OwnerId);
            opponent.ApplyStatusEffect(new StatusEffect("bleed", value: 1, duration: -1));
            var player = state.GetPlayer(source.OwnerId);
            player.NextCardSpeedBonus += 50;
            Debug.Log($"[AcceleratedCutEffect] Applied 1 Bleed and granted +5 speed to next card.");
        }
    }

    // ConsumeEffect — draw cards equal to opponent's bleed stacks, then remove all bleed
    public class ConsumeEffect : ICardEffect
    {
        public void Apply(QueueEntry source, GameState state)
        {
            var player = state.GetPlayer(source.OwnerId);
            var opponent = state.GetOpponent(source.OwnerId);
            var bleed = opponent.StatusEffects.Find(e => e.EffectId == "bleed");
            int draws = bleed != null ? bleed.Value : 0;
            if (draws > 0)
            {
                player.DrawCards(draws, state.Rng);
                opponent.StatusEffects.RemoveAll(e => e.EffectId == "bleed");
                Debug.Log($"[ConsumeEffect] Drew {draws} cards and removed bleed.");
            }
            else
                Debug.Log("[ConsumeEffect] Opponent has no bleed — no draw.");
        }
    }

    // ClarityEffect — clear all debuffs from self, draw 1 card
    public class ClarityEffect : ICardEffect
    {
        public void Apply(QueueEntry source, GameState state)
        {
            var player = state.GetPlayer(source.OwnerId);
            player.StatusEffects.Clear();
            player.DrawCards(1, state.Rng);
            Debug.Log($"[ClarityEffect] Player {source.OwnerId} cleared debuffs and drew 1 card.");
        }
    }

    // DevovitaEffect — destroy card in hand, gain HP equal to its blood cost
    public class DevovitaEffect : ICardEffect, ITargetedEffect
    {
        public void Apply(QueueEntry source, GameState state)
        {
            Debug.Log("[DevovitaEffect] No target provided — no effect.");
        }

        public void ApplyTargeted(QueueEntry source, GameState state, int targetInstanceId)
        {
            var player = state.GetPlayer(source.OwnerId);
            // targetInstanceId is a card InstanceId here, not a permanent
            var card = player.Hand.Find(c => c.InstanceId == targetInstanceId);
            if (card == null)
            {
                Debug.LogWarning($"[DevovitaEffect] Card {targetInstanceId} not in hand.");
                return;
            }
            int heal = Mathf.Min(card.Cost.Blood, player.Character.BaseHP - player.HP);
            player.HP += heal;
            player.Hand.Remove(card);
            // Card destroyed — not added to discard
        }
    }

    // LungeEffect — if MergedQueue[0] after this resolves is opponent's, draw 3
    public class LungeEffect : ICardEffect
    {
        public void Apply(QueueEntry source, GameState state)
        {
            // After this entry was removed, MergedQueue[0] is the next card
            if (state.MergedQueue.Count > 0 && state.MergedQueue[0].OwnerId != source.OwnerId)
            {
                var player = state.GetPlayer(source.OwnerId);
                player.DrawCards(3, state.Rng);
                Debug.Log($"[LungeEffect] Next card is opponent's — Player {source.OwnerId} drew 3.");
            }
            else
                Debug.Log("[LungeEffect] Next card not opponent's — no draw.");
        }
    }

    // TotemOfWardingEffect — spawns Totem of Warding permanent
    public class TotemOfWardingEffect : ICardEffect
    {
        public void Apply(QueueEntry source, GameState state)
        {
            var player = state.GetPlayer(source.OwnerId);
            var totem = new TotemOfWarding(source.OwnerId) { SourceCard = source.Card };
            state.AssignPermanentInstanceId(totem);
            player.Board.Add(totem);
            Debug.Log($"[TotemOfWardingEffect] Player {source.OwnerId} spawned Totem of Warding.");
        }
    }

    // OneForOneEffect — draw a card for each card opponent card in the queue
    public class OneForOneEffect : ICardEffect
    {
        public void Apply(QueueEntry source, GameState state)
        {
            var player = state.GetPlayer(source.OwnerId);
            var opponent = state.GetOpponent(source.OwnerId);
            int opponentQueued = state.MergedQueue.Count(e => e.OwnerId != source.OwnerId);
            if (opponentQueued > 0)
                player.DrawCards(opponentQueued, state.Rng);
            Debug.Log($"[OneForOneEffect] Opponent has {opponentQueued} cards in queue — drew {opponentQueued}.");
        }
    }

    // VampiricForesightEffect — reveal top 3 cards of draw pile, draw one (popup handled client-side)
    // For now draws the top card and sends the top 3 to client via a special state flag
    public class VampiricForesightEffect : ICardEffect
    {
        public void Apply(QueueEntry source, GameState state)
        {
            var player = state.GetPlayer(source.OwnerId);
            if (player.Deck.Count < 3)
            {
                Debug.Log("[VampiricForesightEffect] Not enough cards in deck — reshuffling.");

                if (player.Deck.Count < 3 && player.Discard.Count > 0)
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
                else
                {
                    Debug.Log("[Vampiric Effect] Not enough cards owned.");
                    return;
                }
            }
            // Reveal top 3 — client will show popup; for alpha just draw top card
            int drawCount = Mathf.Min(1, player.Deck.Count);
            player.DrawCards(drawCount, state.Rng);
            Debug.Log($"[VampiricForesightEffect] Player {source.OwnerId} drew top card.");
        }
    }
}