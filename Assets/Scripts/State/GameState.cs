using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FogClouds
{
    // The root server-side game state. One instance lives on the server for the duration of a match.
    // Never serialized and sent to clients in full — use ClientGameStateView for that.

    // All mutations to game state go through GameManager, which validates commands
    // before calling methods on this object.
    [Serializable]
    public class GameState
    {
        //Phase
        public TurnPhase CurrentPhase;
        public int TurnNumber;

        //Players
        public PlayerState Player1;
        public PlayerState Player2;

        // Queues
        // Player 1's personal queue during MainPhase.

        public List<QueueEntry> Player1Queue;

        //Player 2's personal queue during MainPhase.
        public List<QueueEntry> Player2Queue;

        // The merged, sorted queue used during QueueResolution.
        // Null outside of QueueResolution phase.
        public List<QueueEntry> MergedQueue;

        //Roguelike
        public RoguelikeState RoguelikeState;

        public AuctionOffer AuctionOffer;

        public ShopOffer Player0ShopOffer;
        public ShopOffer Player1ShopOffer;

        public string CurrentEventId;          // set at EnterEventPhase
        public string[] PlayerEventChoices;    // length 2, null = not submitted

        // Temporary storage for Writing on the Wall revealed cards, indexed by player
        public List<CardInstance>[] EventRevealedCards;

        //For events that have outcomes (e.g. Fortune Favors the Bold)
        public string EventOutcome; //set at EnterEventPhase

        private int _nextInstanceId = 10000;
        public int GenerateInstanceId() => _nextInstanceId++;

        //Win Condition
        public bool GameOver;
        public int WinnerPlayerId; // -1 = no winner yet

        //RNG
        // Server-side seeded RNG. Used for speed tie resolution, offer generation, etc.
        // Seed is logged at game start for replay/debug.

        [NonSerialized]
        public System.Random Rng;

        //Constructor

        public GameState() { }

        public GameState(CharacterData p1Character, CharacterData p2Character, int rngSeed = 0)
        {

            Player1 = new PlayerState(0, p1Character);
            Player2 = new PlayerState(1, p2Character);

            Player1Queue = new List<QueueEntry>();
            Player2Queue = new List<QueueEntry>();
            MergedQueue = null;

            RoguelikeState = new RoguelikeState();

            AuctionOffer = new AuctionOffer();

            PlayerEventChoices = new string[2];

            Player0ShopOffer = new ShopOffer();
            Player1ShopOffer = new ShopOffer();

            EventRevealedCards = new List<CardInstance>[2];

            CurrentPhase = TurnPhase.TurnStart;
            TurnNumber = 1;
            GameOver = false;
            WinnerPlayerId = -1;

            int seed = rngSeed == 0 ? Environment.TickCount : rngSeed;
            Rng = new System.Random(seed);
            UnityEngine_Debug_Log($"GameState initialized. RNG seed: {seed}");
        }

        //Player Accessors
        public PlayerState GetPlayer(int playerId) => playerId == 0 ? Player1 : Player2;
        public PlayerState GetOpponent(int playerId) => playerId == 0 ? Player2 : Player1;
        public List<QueueEntry> GetQueue(int playerId) => playerId == 0 ? Player1Queue : Player2Queue;

        //Snapshot

        // Takes a frozen snapshot of each player's state and stores it on the opponent's
        // TurnStartSnapshot field. Called at the TurnStart → MainPhase transition,
        // after draw and resource refresh, before any player input.
        // All fog filter reads during this main phase use these snapshots.
        public void TakeSnapshots()
        {
            // Each player's snapshot holds the OPPONENT's state
            Player1.TurnStartSnapshot = PlayerSnapshot.From(Player2);
            Player2.TurnStartSnapshot = PlayerSnapshot.From(Player1);
        }

        //Queue Operations

        // Adds a card to the specified player's queue and re-sorts by speed (descending).
        // Called during MainPhase when a player plays a Queueable card.
        public void EnqueueCard(int playerId, CardInstance card, bool wasUpcast = false)
        {
            var queue = GetQueue(playerId);
            var player = GetPlayer(playerId);

            int speed = card.ModifiedSpeed;
            int bonusDamage = 0;

            foreach (var passive in player.Passives)
            {
                switch (passive.PassiveId)
                {
                    case "blessing_of_grace":
                        speed += 2 * passive.StackCount;
                        break;
                    case "blessing_of_valor":
                        bonusDamage += 1 * passive.StackCount;
                        break;
                    case "pact_of_the_devil":
                        bonusDamage += passive.StackCount;
                        break;
                }
            }

            var entry = new QueueEntry(playerId, card, speed)
            {
                TieBreaker = Rng.Next(),
                BonusDamage = bonusDamage,
                WasUpcast = wasUpcast
            };

            queue.Add(entry);
            SortPlayerQueue(playerId);
        }

        // Re-sorts a player's personal queue by CurrentSpeed descending.
        // Called when: a card is enqueued, an instant modifies speed, a passive triggers.
        // Ties broken randomly using server RNG.
        public void SortPlayerQueue(int playerId)
        {
            var queue = GetQueue(playerId);
            // Stable descending sort with random tie-breaking
            var sorted = queue
                .OrderByDescending(e => e.CurrentSpeed)
                .ThenBy(e => e.TieBreaker)
                .ToList();
            queue.Clear();
            queue.AddRange(sorted);
        }

        // Merges both player queues into MergedQueue, sorted descending by CurrentSpeed.
        // Ties broken randomly. Called at QueueMerge phase.
        // Queue order is locked after this call — no re-sorting during resolution.
        public void MergeQueues()
        {
            var combined = Player1Queue.Concat(Player2Queue).ToList();
            MergedQueue = combined
                .OrderByDescending(e => e.CurrentSpeed)
                .ThenBy(e => e.TieBreaker)
                .ToList();

            Player1Queue.Clear();
            Player2Queue.Clear();
        }

        //Win Condition

        // Checks if either player has reached 0 HP. Sets GameOver and WinnerPlayerId.
        // Should be called after any effect that deals damage.
        public void CheckWinCondition()
        {
            bool p1Dead = !Player1.IsAlive;
            bool p2Dead = !Player2.IsAlive;

            if (p1Dead && p2Dead)
            {
                // Simultaneous kill — revive both at 1 HP and continue
                Player1.HP = 1;
                Player2.HP = 1;
            }
            else if (p1Dead)
            {
                GameOver = true;
                WinnerPlayerId = 1;
            }
            else if (p2Dead)
            {
                GameOver = true;
                WinnerPlayerId = 0;
            }
        }

        //Utility

        // Thin wrapper so pure C# tests can run without Unity present.
        // GameManager replaces this with Debug.Log in the Unity context.
        private static Action<string> _logger = Console.WriteLine;
        public static void SetLogger(Action<string> logger) => _logger = logger;
        private static void UnityEngine_Debug_Log(string msg) => _logger?.Invoke($"[GameState] {msg}");

        public void DestroyPermanent(int destroyerPlayerId, BoardPermanent target)
        {
            var owner = GetPlayer(target.OwnerId);
            var destroyer = GetPlayer(destroyerPlayerId);

            if (!owner.Board.Remove(target)) return;

            if (target.SourceCard != null) { owner.Discard.Add(target.SourceCard); }

            destroyer.Silver += 3;

            Debug.Log($"[GameState] Player {destroyerPlayerId} destroyed {target.DisplayName}, earned 3 Silver.");
        }
    }
}
