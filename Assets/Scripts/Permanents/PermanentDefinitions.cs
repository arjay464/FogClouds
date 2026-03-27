using System;
using UnityEngine;

namespace FogClouds
{
    //Permanent Interfaces

    public interface IDamageModifier
    {
        int ModifyDamage(int damage, bool isAttacker);
    }

    public interface IDrawModifier
    {
        int ModifyDraw(int drawCount);
    }

    [Serializable]
    public class BoardPermanent
    {
        public string PermanentId;
        public string DisplayName;
        public int OwnerId;

        // Set true by Lex Noctis — prevents destruction this turn. Cleared at TurnStart.
        public bool ProtectedThisTurn;

        //Turns remaining before this permanent expires. -1 = lasts until removed.
        public int TurnsRemaining;

        public CardInstance SourceCard;

        //Stub for phase 0 - board permanents should have some effect defined

        public BoardPermanent() { }

        public virtual BoardPermanent Clone()
        {
            return new BoardPermanent
            {
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

        public int ModifyDamage(int damage, bool isAttacker)
        {
            if (!isAttacker) return damage;
            return damage + BonusDamage;
        }

        public override BoardPermanent Clone()
        {
            return new ChaCha(OwnerId, BonusDamage)
            {
                TurnsRemaining = this.TurnsRemaining
            };
        }
    }

    // Cursed Goblet
    // Reduces incoming damage by a percentage, rounded up (favorable to defender).
    // Duration: permanent until destroyed.
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

        public int ModifyDamage(int damage, bool isAttacker)
        {
            if (isAttacker) return damage;
            return Mathf.FloorToInt(damage * (1f - ReductionPercent));
        }

        public override BoardPermanent Clone()
        {
            return new CursedGoblet(OwnerId, ReductionPercent)
            {
                TurnsRemaining = this.TurnsRemaining
            };
        }
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

        // Called from DealDamageEffect — needs the source entry to check IsAttack + upcast
        // For now uses isAttacker flag; full upcast check requires passing QueueEntry
        public int ModifyDamage(int damage, bool isAttacker)
        {
            if (!isAttacker) return damage;
            return damage + BonusDamage;
        }

        public override BoardPermanent Clone() =>
            new TotemOfSharpness(OwnerId, BonusDamage, TurnsRemaining);
    }

    [Serializable]
    public class MirrorOfMoonlight : BoardPermanent
    {
        public MirrorOfMoonlight(int ownerId, int turnsRemaining)
        {
            PermanentId = "mirror_of_moonlight";
            DisplayName = "Mirror of Moonlight";
            OwnerId = ownerId;
            TurnsRemaining = turnsRemaining;
        }


        public override BoardPermanent Clone() =>
            new MirrorOfMoonlight(OwnerId, TurnsRemaining);
    }

    // Totem of Sacrifice — reduces blood costs by 1 (minimum 1), infinite duration
    [Serializable]
    public class TotemOfSacrifice : BoardPermanent
    {
        public TotemOfSacrifice(int ownerId)
        {
            PermanentId = "totem_of_sacrifice";
            DisplayName = "Totem of Sacrifice";
            OwnerId = ownerId;
            TurnsRemaining = -1;
        }

        public override BoardPermanent Clone() =>
            new TotemOfSacrifice(OwnerId) { TurnsRemaining = this.TurnsRemaining };
    }

    // Totem of Progress — upcast cards draw 1 card on resolution
    [Serializable]
    public class TotemOfProgress : BoardPermanent
    {
        public TotemOfProgress(int ownerId, int turnsRemaining)
        {
            PermanentId = "totem_of_progress";
            DisplayName = "Totem of Progress";
            OwnerId = ownerId;
            TurnsRemaining = turnsRemaining;
        }

        public override BoardPermanent Clone() =>
            new TotemOfProgress(OwnerId, TurnsRemaining);
    }
}