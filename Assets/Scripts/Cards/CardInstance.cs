using UnityEngine;
using System;

namespace FogClouds
{
    // A runtime instance of a card. Wraps a CardDefinition with mutable, per-instance state.
    // Every card in a player's hand, deck, discard, or queue is a CardInstance.
    [Serializable]
    public class CardInstance
    {
        // Unique identifier for this specific instance. Generated at game start.
        // Used to track cards across hand/queue/discard without ambiguity.
        public int InstanceId;

        // The immutable definition this instance is based on.
        public string CardId;

        // Current speed value. Starts equal to Definition.BaseSpeed.
        // Can be modified by card effects while in queue.
        // Only meaningful for Queueable cards.

        public string DisplayName;
        public int ModifiedSpeed;

        public int BaseSpeed;
        public bool IsAttack;
        public CardType Type;

        public ResourceCost Cost;

        public string EffectId;
        public bool WasUpcast;

        public CardInstance() { }

        public CardInstance(CardDefinition definition, int instanceId)
        {
            CardId = definition.CardId;
            InstanceId = instanceId;
            ModifiedSpeed = definition.BaseSpeed;
            BaseSpeed = definition.BaseSpeed;
            Type = definition.Type;
            Cost = definition.Cost;
            EffectId = definition.EffectId;
            DisplayName = definition.DisplayName;
            IsAttack = definition.IsAttack;
            WasUpcast = false;
        }

        //Resets ModifiedSpeed back to the base value from the definition.
        public void ResetSpeed()
        {
            ModifiedSpeed = BaseSpeed;
        }

        //Returns a deep copy of this instance.
        //Used when building TurnStartSnapshot — the snapshot must not share references with live state.
        public CardInstance Clone()
        {
            return new CardInstance
            {
                InstanceId = this.InstanceId,
                CardId = this.CardId,
                ModifiedSpeed = this.ModifiedSpeed,
                BaseSpeed = this.BaseSpeed,
                Type = this.Type,
                Cost = this.Cost,
                EffectId = this.EffectId,
                DisplayName = this.DisplayName,
                IsAttack = this.IsAttack,
                WasUpcast = this.WasUpcast
            };
        }

        public override string ToString()
        {
            return $"[{InstanceId}] {CardId ?? "Unknown"} (spd:{ModifiedSpeed})";
        }
    }
}
