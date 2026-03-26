using System;
using UnityEngine;

namespace FogClouds
{
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

            // Blessed by the Storm status effect
            var attacker2 = state.GetPlayer(source.OwnerId);
            var stormBuff = attacker2.StatusEffects.Find(e => e.EffectId == "blessed_by_storm");
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

            // Store pre-shield damage for lifesteal calculation
            int preShieldDamage = damage;
            int shieldBefore = defender.Shield;  // capture before mutation
            defender.TakeDamage(damage);

            int actualDamage = Math.Max(0, preShieldDamage - shieldBefore);  // use captured value

            if (actualDamage > 0)
                attacker.Silver += actualDamage;

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
            player.Board.Add(totem);
            Debug.Log($"[TotemOfSacrificeEffect] Player {source.OwnerId} spawned Totem of Sacrifice.");
        }
    }

    // FerricidiumEffect — instant: destroys target permanent on opponent's board
    // For alpha, destroys the first permanent found. Targeting UI comes later.
    public class FerricidiumEffect : ICardEffect
    {
        public void Apply(QueueEntry source, GameState state)
        {
            var opponent = state.GetOpponent(source.OwnerId);
            var target = opponent.Board.Find(p => !p.ProtectedThisTurn);
            if (target == null)
            {
                Debug.Log("[FerricidiumEffect] No unprotected permanents to destroy.");
                return;
            }
            state.DestroyPermanent(source.OwnerId, target);
            Debug.Log($"[FerricidiumEffect] Destroyed {target.DisplayName}.");
        }
    }

    // LexNoctisEffect — instant: protects target permanent this turn
    // Sets LexNoctisProtected flag on the first permanent found. Targeting UI comes later.
    public class LexNoctisEffect : ICardEffect
    {
        public void Apply(QueueEntry source, GameState state)
        {
            var player = state.GetPlayer(source.OwnerId);
            if (player.Board.Count == 0)
            {
                Debug.Log($"[LexNoctisEffect] No permanents to protect.");
                return;
            }
            player.Board[0].ProtectedThisTurn = true;
            Debug.Log($"[LexNoctisEffect] Protected {player.Board[0].DisplayName}.");
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
}