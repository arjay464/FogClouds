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

        //Turns remaining before this permanent expires. -1 = lasts until removed.
        public int TurnsRemaining;

        //Stub for phase 0 - board permanents should have some effect defined

        public BoardPermanent() { }

        public virtual BoardPermanent Clone()
        {
            return new BoardPermanent
            {
                PermanentId = this.PermanentId,
                DisplayName = this.DisplayName,
                OwnerId = this.OwnerId,
                TurnsRemaining = this.TurnsRemaining
            };
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
            return Mathf.CeilToInt(damage * (1f - ReductionPercent));
        }

        public override BoardPermanent Clone()
        {
            return new CursedGoblet(OwnerId, ReductionPercent)
            {
                TurnsRemaining = this.TurnsRemaining
            };
        }
    }
}