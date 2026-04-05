namespace FogClouds
{
    public interface IPassiveEffect
    {
        // Called once when the passive is first acquired (Shop purchase or upgrade)
        void OnAcquire(PlayerState player, GameState state);

        // Called at TurnStart before draw and resource refresh
        void OnTurnStart(PlayerState player, GameState state);

        // Called after this player takes HP damage (post-shield)
        void OnDamageTaken(PlayerState player, int amount, GameState state);

        // Called when this player plays any card from hand
        void OnCardPlayed(PlayerState player, CardInstance card, GameState state);

        //Fires when this player takes damage (not hp loss)
        void OnHPDamaged(PlayerState player, int amount, GameState state);

        // Fires when a permanent owned by this player is destroyed by an opponent
        void OnPermanentDestroyed(PlayerState player, BoardPermanent permanent, int destroyerPlayerId, GameState state);

        // True if this passive requires player interaction to activate each turn
        bool IsInteractive { get; }
    }
}