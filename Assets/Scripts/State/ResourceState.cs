using UnityEngine;
using System;

namespace FogClouds
{
    // Holds a character's two resource tracks.
    // Specific characters name these resources differently (e.g. "Mana", "Blood", "Gold"),
    // but all characters use this same underlying structure. Named aliases live in CharacterData.
    [Serializable]
    public class ResourceState
    {
        // Refreshes to its maximum value at the start of each turn.
        // Cannot be banked between turns — excess is lost.
        public int PerTurnResource;

        // Maximum value for PerTurnResource. Set by character definition.
        // May be modified by upgrades.
        public int PerTurnResourceMax;

        // Carries over between turns. Not auto-generated — must be earned through effects.
        // Has no inherent maximum unless a card/upgrade imposes one.
        public int PersistentResource;

        public int BonusPerTurnResource;

        public ResourceState() { }

        public ResourceState(int perTurnMax, int persistent = 0)
        {
            PerTurnResourceMax = perTurnMax;
            PerTurnResource = perTurnMax;
            PersistentResource = persistent;
            BonusPerTurnResource = 0;
        }
        // Resets PerTurnResource to its maximum. Called at TurnStart.
        public void RefreshPerTurn()
        {
            PerTurnResource = PerTurnResourceMax + BonusPerTurnResource;
        }

        // Returns a deep copy. Used when building TurnStartSnapshot.
        public ResourceState Clone()
        {
            return new ResourceState
            {
                PerTurnResource = this.PerTurnResource,
                PerTurnResourceMax = this.PerTurnResourceMax,
                PersistentResource = this.PersistentResource,
                BonusPerTurnResource = this.BonusPerTurnResource
            };
        }
    }
}
