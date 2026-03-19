using UnityEngine;
using System;

namespace FogClouds
{
    // Tracks what information Player A has revealed about Player B.
    // Each player holds one FogRevealState representing their view of the opponent.

    // All flags are read-only to clients. Only the server sets them (via INSIGHT tree unlocks
    // or card effects). All revealed information is sourced from TurnStartSnapshot, never live state.

    [Serializable]
    public class FogRevealState
    {
        //Identity
        //Who the opponent is playing. Required before face damage can be dealt.
        public bool CharacterIdentity;

        //Vitals
        //Opponent's current HP (as of turn start).
        public bool CharacterHP;

        //Opponent's resource amounts (as of turn start).
        public bool CharacterResources;

        //Hand
        //Number of cards in opponent's hand. Tier 1 of hand reveals.
        public bool HandSize;

        //Full contents of opponent's hand. Tier 2 — requires HandSize first.
        public bool HandContents;

        //Draw Pile
        //Number of cards remaining in opponent's draw pile. Tier 1.
        public bool DrawPileCount;

        //Full contents of opponent's draw pile. Tier 2 — requires DrawPileCount first.
        public bool DrawPileContents;

        public bool DrawPileOrdered;

        //Discard Pile
        //Number of cards in opponent's discard pile. Tier 1.
        public bool DiscardPileCount;

        //Full contents of opponent's discard pile. Tier 2 — requires DiscardPileCount first.
        public bool DiscardPileContents;

        //Board
        //Opponent's permanents and creatures. Staged reveal granularity TBD.
        public bool PermanentsOpponentCount;
        public bool BoardState;

        //Passives
        //Number of passives the opponent has active. Tier 1.
        public bool PassivesOpponentCount;

        //Full details of opponent's active passives. Tier 2 — requires PassivesOpponentCount first.
        public bool PassivesOpponent;

        //Upgrades
        //Upgrades the opponent has chosen in previous turns.
        public bool PastUpgrades;

        //What upgrade options will be offered to the opponent next turn (pre-generated).
        public bool FutureUpgradesOpponent;

        //What upgrade options will be offered to you next turn (pre-generated).
        public bool FutureUpgradesSelf;

        //INSIGHT 
        //What nodes the opponent has unlocked in the INSIGHT tree.
        public bool InsightTreeOpponent;

        //Helpers
        // Returns true if all flags are false (full fog — game start state).
        public bool IsFullFog()
        {
            return !CharacterIdentity
                && !CharacterHP
                && !CharacterResources
                && !HandSize
                && !HandContents
                && !DrawPileCount
                && !DrawPileContents
                && !DrawPileOrdered
                && !DiscardPileCount
                && !DiscardPileContents
                && !PermanentsOpponentCount
                && !BoardState
                && !PassivesOpponentCount
                && !PassivesOpponent
                && !PastUpgrades
                && !FutureUpgradesOpponent
                && !FutureUpgradesSelf
                && !InsightTreeOpponent;
        }

        // Returns a deep copy. Used when building TurnStartSnapshot.
        public FogRevealState Clone()
        {
            return new FogRevealState
            {
                CharacterIdentity = this.CharacterIdentity,
                CharacterHP = this.CharacterHP,
                CharacterResources = this.CharacterResources,
                HandSize = this.HandSize,
                HandContents = this.HandContents,
                DrawPileCount = this.DrawPileCount,
                DrawPileContents = this.DrawPileContents,
                DrawPileOrdered = this.DrawPileOrdered,
                DiscardPileCount = this.DiscardPileCount,
                DiscardPileContents = this.DiscardPileContents,
                PermanentsOpponentCount = this.PermanentsOpponentCount,
                BoardState = this.BoardState,
                PassivesOpponentCount = this.PassivesOpponentCount,
                PassivesOpponent = this.PassivesOpponent,
                PastUpgrades = this.PastUpgrades,
                FutureUpgradesOpponent = this.FutureUpgradesOpponent,
                FutureUpgradesSelf = this.FutureUpgradesSelf,
                InsightTreeOpponent = this.InsightTreeOpponent
            };
        }
    }
}
