using UnityEngine;
namespace FogClouds
{
    // Implemented by every card effect class.
    // Effects receive the full server-side GameState and the QueueEntry that triggered them.
    // Effects must never read from TurnStartSnapshot — they operate on live state.
    public interface ICardEffect
    {
        // Apply this card's effect to the game state.
        // Called by the resolution engine during QueueResolution, or immediately for Instants.
        // Source: The QueueEntry (or null for Instants) that triggered this effect.
        // State: The full server-side game state. Mutate this directly.
        void Apply(QueueEntry source, GameState state);
    }
}
