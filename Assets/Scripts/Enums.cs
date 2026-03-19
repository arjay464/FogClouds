using UnityEngine;
namespace FogClouds
{
    // The six phases of a FogClouds turn, in order.
    // The server is the sole authority on phase transitions.

    public enum TurnPhase
    {
        TurnStart,
        MainPhase,
        QueueMerge,
        QueueResolution,
        RoguelikePhase,
        ShopPhase,
        AuctionPhase,
        EventPhase,
        TurnEnd
    }

    // Determines how a card interacts with the queue and board.

    public enum CardType
    {
        // Added to the player's personal queue. Resolves by speed at end of main phase.
        Queueable,

        // Played immediately during main phase. Affects only own board/queue. Does not use queue.
        Instant,

        // Removed from deck permanently (Slay the Spire style). Persists on board.
        Permanent
    }

    // Roguelike investment category chosen each turn.
    public enum RoguelikeCategory
    {
        // Random pool. Improves damage output and board state
        Power,

        // Random pool. Improves deck control, draw, hand management.
        Strategy,

        //Fixed tech tree. Unlocks fog reveal flags.
        Insight
    }

    // Result of a server-side command validation.
    public enum CommandResult
    {
        Success,
        InvalidPhase,
        InsufficientResources,
        CardNotInHand,
        NotAuthorized,
        DuplicateCapExceeded
    }

    public enum ShopPurchaseType
    {
        PowerCard,
        StrategyCard,
        ColorlessCard,
        Passive,
        HpRegenSmall,
        HpRegenLarge,
        SightSmall,
        SightLarge,
        PersistentResource
    }
}
