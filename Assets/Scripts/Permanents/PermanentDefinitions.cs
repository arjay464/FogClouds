using System;
using UnityEngine;

namespace FogClouds
{
    // ——— PERMANENT INTERFACES ———————————————————————————

    // Modifies outgoing or incoming damage in DealDamageEffect.
    public interface IDamageModifier
    {
        int ModifyDamage(int damage, bool isAttacker);
    }

    public interface IDrawModifier
    {
        int ModifyDraw(int drawCount);
    }

    // Fires when owner takes HP damage from opponent.
    public interface IDamageTakenReactor
    {
        void OnDamageTakenFromOpponent(PlayerState owner, PlayerState attacker, int amount, GameState state);
    }

    // Modifies blood costs in GameManager.DeductCost.
    public interface IBloodCostModifier
    {
        int ModifyBloodCost(int cost);
    }

    // Fires after each card resolves during QueueResolution.
    public interface IOnCardResolved
    {
        void OnCardResolved(QueueEntry entry, PlayerState owner, GameState state);
    }

    // ——— BASE CLASS —————————————————————————————————————

    [Serializable]
    public class BoardPermanent
    {
        public int InstanceId;
        public string PermanentId;
        public string DisplayName;
        public int OwnerId;

        // Set true by Lex Noctis — prevents destruction this turn. Cleared at TurnStart.
        public bool ProtectedThisTurn;

        // Turns remaining before this permanent expires. -1 = lasts until removed.
        public int TurnsRemaining;

        public CardInstance SourceCard;

        public BoardPermanent() { }

        // Called by PlayerState.AddPermanent — subclasses register into indexed collections here.
        // Never called on cloned instances (snapshots don't need indices).
        public virtual void OnAdded(PlayerState player) { }

        // Called by PlayerState.RemovePermanent — subclasses deregister from indexed collections here.
        public virtual void OnRemoved(PlayerState player) { }

        public virtual BoardPermanent Clone()
        {
            return new BoardPermanent
            {
                InstanceId = this.InstanceId,
                PermanentId = this.PermanentId,
                DisplayName = this.DisplayName,
                OwnerId = this.OwnerId,
                TurnsRemaining = this.TurnsRemaining,
                ProtectedThisTurn = false,
                SourceCard = this.SourceCard
            };
        }

        public virtual void OnTurnStart()
        {
            ProtectedThisTurn = false;
        }
    }

    // ——— INERT PERMANENT ————————————————————————————————
    // For permanents whose behavior is fully handled at play time (e.g. MirrorOfMoonlight).
    // No board-side hooks needed — OnAdded/OnRemoved are no-ops from the base.
    [Serializable]
    public class InertPermanent : BoardPermanent
    {
        public InertPermanent(string permanentId, string displayName, int ownerId, int turnsRemaining)
        {
            PermanentId = permanentId;
            DisplayName = displayName;
            OwnerId = ownerId;
            TurnsRemaining = turnsRemaining;
        }

        public override BoardPermanent Clone() =>
            new InertPermanent(PermanentId, DisplayName, OwnerId, TurnsRemaining)
            {
                InstanceId = this.InstanceId,
                SourceCard = this.SourceCard
            };
    }

    //Permanent Subclasses

    // Cha Cha - Loyal Chupacabra
    // Buffs outgoing attack damage by a flat amount. Duration: 2 turns.
    [Serializable]
    public class ChaCha : BoardPermanent, IDamageModifier
    {
        public int BonusDamage;

        public ChaCha(int ownerId, int bonusDamage)
        {
            PermanentId = "cha_cha";
            DisplayName = "Cha Cha - Loyal Chupacabra";
            OwnerId = ownerId;
            TurnsRemaining = 2;
            BonusDamage = bonusDamage;
        }

        public override void OnAdded(PlayerState player) => player.DamageModifiers.Add(this);
        public override void OnRemoved(PlayerState player) => player.DamageModifiers.Remove(this);

        public int ModifyDamage(int damage, bool isAttacker)
        {
            if (!isAttacker) return damage;
            return damage + BonusDamage;
        }

        public override BoardPermanent Clone() =>
            new ChaCha(OwnerId, BonusDamage) { InstanceId = this.InstanceId, TurnsRemaining = this.TurnsRemaining, SourceCard = this.SourceCard };
    }

    // Cursed Goblet
    // Reduces incoming damage by a percentage, rounded down. Duration: permanent until destroyed.
    [Serializable]
    public class CursedGoblet : BoardPermanent, IDamageModifier
    {
        public float ReductionPercent;

        public CursedGoblet(int ownerId, float reductionPercent)
        {
            PermanentId = "cursed_goblet";
            DisplayName = "Cursed Goblet";
            OwnerId = ownerId;
            TurnsRemaining = -1;
            ReductionPercent = reductionPercent;
        }

        public override void OnAdded(PlayerState player) => player.DamageModifiers.Add(this);
        public override void OnRemoved(PlayerState player) => player.DamageModifiers.Remove(this);

        public int ModifyDamage(int damage, bool isAttacker)
        {
            if (isAttacker) return damage;
            return Mathf.FloorToInt(damage * (1f - ReductionPercent));
        }

        public override BoardPermanent Clone() =>
            new CursedGoblet(OwnerId, ReductionPercent) { InstanceId = this.InstanceId, TurnsRemaining = this.TurnsRemaining, SourceCard = this.SourceCard };
    }
    // Totem of Sharpness — attacks costing a dagger or that were upcast gain +3 damage
    [Serializable]
    public class TotemOfSharpness : BoardPermanent, IDamageModifier
    {
        public int BonusDamage;

        public TotemOfSharpness(int ownerId, int bonusDamage, int turnsRemaining)
        {
            PermanentId = "totem_of_sharpness";
            DisplayName = "Totem of Sharpness";
            OwnerId = ownerId;
            TurnsRemaining = turnsRemaining;
            BonusDamage = bonusDamage;
        }

        public override void OnAdded(PlayerState player) => player.DamageModifiers.Add(this);
        public override void OnRemoved(PlayerState player) => player.DamageModifiers.Remove(this);

        public int ModifyDamage(int damage, bool isAttacker)
        {
            if (!isAttacker) return damage;
            return damage + BonusDamage;
        }

        public override BoardPermanent Clone() =>
            new TotemOfSharpness(OwnerId, BonusDamage, TurnsRemaining) { InstanceId = this.InstanceId, SourceCard = this.SourceCard };
    }

    // Mirror of Moonlight — copies instants into the queue at speed 2.
    // Behavior is handled entirely at play time in GameManager; no board-side hook needed.
    [Serializable]
    public class MirrorOfMoonlight : InertPermanent
    {
        public MirrorOfMoonlight(int ownerId, int turnsRemaining)
            : base("mirror_of_moonlight", "Mirror of Moonlight", ownerId, turnsRemaining) { }

        public override BoardPermanent Clone() =>
            new MirrorOfMoonlight(OwnerId, TurnsRemaining) { InstanceId = this.InstanceId, SourceCard = this.SourceCard };
    }

    // Totem of Sacrifice — reduces blood costs by 1, minimum 1. Duration: infinite.
    [Serializable]
    public class TotemOfSacrifice : BoardPermanent, IBloodCostModifier
    {
        public TotemOfSacrifice(int ownerId)
        {
            PermanentId = "totem_of_sacrifice";
            DisplayName = "Totem of Sacrifice";
            OwnerId = ownerId;
            TurnsRemaining = -1;
        }

        public override void OnAdded(PlayerState player) => player.BloodCostModifiers.Add(this);
        public override void OnRemoved(PlayerState player) => player.BloodCostModifiers.Remove(this);

        public int ModifyBloodCost(int cost) => Mathf.Max(1, cost - 1);

        public override BoardPermanent Clone() =>
            new TotemOfSacrifice(OwnerId) { InstanceId = this.InstanceId, TurnsRemaining = this.TurnsRemaining, SourceCard = this.SourceCard };
    }

    // Totem of Progress — upcast cards draw 1 card on resolution. Duration: 3 turns.
    [Serializable]
    public class TotemOfProgress : BoardPermanent, IOnCardResolved
    {
        public TotemOfProgress(int ownerId, int turnsRemaining)
        {
            PermanentId = "totem_of_progress";
            DisplayName = "Totem of Progress";
            OwnerId = ownerId;
            TurnsRemaining = turnsRemaining;
        }

        public override void OnAdded(PlayerState player) => player.OnCardResolvedListeners.Add(this);
        public override void OnRemoved(PlayerState player) => player.OnCardResolvedListeners.Remove(this);

        public void OnCardResolved(QueueEntry entry, PlayerState owner, GameState state)
        {
            if (!entry.WasUpcast) return;
            owner.DrawCards(1, state.Rng);
            Debug.Log($"[TotemOfProgress] Player {owner.PlayerId} drew 1 card from upcast.");
        }

        public override BoardPermanent Clone() =>
            new TotemOfProgress(OwnerId, TurnsRemaining) { InstanceId = this.InstanceId, SourceCard = this.SourceCard };
    }

    // Totem of Warding — when opponent deals HP damage to you, apply 1 bleed to them. Duration: 2.
    [Serializable]
    public class TotemOfWarding : BoardPermanent, IDamageTakenReactor
    {
        public TotemOfWarding(int ownerId)
        {
            PermanentId = "totem_of_warding";
            DisplayName = "Totem of Warding";
            OwnerId = ownerId;
            TurnsRemaining = 2;
        }

        public override void OnAdded(PlayerState player) => player.DamageTakenReactors.Add(this);
        public override void OnRemoved(PlayerState player) => player.DamageTakenReactors.Remove(this);

        public void OnDamageTakenFromOpponent(PlayerState owner, PlayerState attacker, int amount, GameState state)
        {
            attacker.ApplyStatusEffect(new StatusEffect("bleed", value: 1, duration: -1));
            Debug.Log($"[TotemOfWarding] Applied 1 Bleed to Player {attacker.PlayerId}.");
        }

        public override BoardPermanent Clone() =>
            new TotemOfWarding(OwnerId) { InstanceId = this.InstanceId, TurnsRemaining = this.TurnsRemaining, SourceCard = this.SourceCard };
    }
}