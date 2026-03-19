using System;

namespace FogClouds
{
    public interface IGameEventEffect
    {
        bool IsInteractive { get; }
        void Apply(GameState state);
        bool ValidateChoice(GameState state, int playerId, string choice, out string rejectionReason);
        void ApplyChoice(GameState state, int playerId, string choice);
        bool IsResolved(GameState state);
    }
}
