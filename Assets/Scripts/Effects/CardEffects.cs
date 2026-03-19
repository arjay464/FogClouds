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

            int damage = Amount;

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
            defender.TakeDamage(damage);

            // Actual HP damage is what made it past the shield
            int actualDamage = Math.Max(0, preShieldDamage - defender.Shield);

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
}