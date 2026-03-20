using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FogClouds
{

    // Lightweight card representation for network transmission.
    // Contains only display/gameplay data — no CardDefinition reference.
    // The client looks up the full asset by CardId if needed.
    [Serializable]
    public class CardInstanceView
    {
        public int InstanceId;
        public string CardId;
        public string DisplayName;
        public int ModifiedSpeed;
        public CardType Type;
        public ResourceCost Cost;
    }

    // The complete state packet sent to a client each time state is broadcast.
    // Own state is live and unfiltered. Opponent state is fog-filtered from TurnStartSnapshot.
    [Serializable]
    public class ClientGameStateView
    {
        public TurnPhase CurrentPhase;
        public int TurnNumber;

        // This client's own state. Full, unfiltered, live.
        public PlayerStateView OwnState;

        // The opponent's state. Fog-filtered from TurnStartSnapshot.
        public PlayerStateView OpponentState;

        // This client's own queue. Always visible.
        public List<QueueEntryView> OwnQueue;

        // The merged queue during QueueResolution. Null outside of QueueResolution.
        public List<QueueEntryView> MergedQueue;

        public bool GameOver;
        public int WinnerPlayerId;

    }

    // A player's state as seen by a client. May contain sentinel values (-1 / null)
    // where the fog is still in effect.
    [Serializable]
    public class PlayerStateView
    {
        public int PlayerId;

        // client loads CharacterData asset locally by this ID
        public string CharacterId;

        // -1 if CharacterHP flag is false.
        public int HP;

        // -1 if CharacterHP flag is false (shield tied to HP reveal).
        public int Shield;

        //Can never reveal opponent's silver as of alpha.
        public int Silver;

        // Null if CharacterResources flag is false.
        public ResourceState Resources;

        // -1 if HandSize flag is false for opponent.
        public int HandSize;

        // Null if HandContents flag is false.
        public List<CardInstanceView> Hand;

        // -1 if DrawPileCount flag is false.
        public int DeckCount;

        // Null if DrawPileContents flag is false.
        public List<CardInstanceView> Deck;

        // -1 if DiscardPileCount flag is false.
        public int DiscardCount;

        // Null if DiscardPileContents flag is false.
        public List<CardInstanceView> Discard;

        // Null if BoardState flag is false.
        public List<BoardPermanent> Board;

        // -1 if PassivesOpponentCount flag is false.
        public int PassiveCount;

        // Null if PassivesOpponent flag is false.
        public List<Passive> Passives;

        // Null if PastUpgrades flag is false.
        public List<string> PastUpgrades;

        // Null if FutureUpgradesOpponent flag is false.
        public RoguelikeOffers FutureOffers;

        // Null if InsightTreeOpponent flag is false.
        public InsightTreeState InsightTree;

        public bool UpgradeChoiceSubmitted;
        public bool PowerCategoryCommitted;
        public bool StrategyCategoryCommitted;
    }

    // A queue entry as seen by a client.
    [Serializable]
    public class QueueEntryView
    {
        public int OwnerId;
        public int CurrentSpeed;
        public int QueuePosition;
        public CardInstanceView Card;
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // FOG FILTER
    // ─────────────────────────────────────────────────────────────────────────────

    public static class FogFilter
    {
        public static ClientGameStateView GenerateView(GameState state, int viewerPlayerId)
        {
            PlayerState viewer = state.GetPlayer(viewerPlayerId);

            return new ClientGameStateView
            {
                CurrentPhase = state.CurrentPhase,
                TurnNumber = state.TurnNumber,
                OwnState = BuildOwnView(viewer),
                OpponentState = BuildOpponentView(viewer),
                OwnQueue = BuildQueueView(state.GetQueue(viewerPlayerId)),
                MergedQueue = state.MergedQueue != null
                                    ? BuildQueueView(state.MergedQueue)
                                    : null,
                GameOver = state.GameOver,
                WinnerPlayerId = state.WinnerPlayerId
            };
        }

        private static PlayerStateView BuildOwnView(PlayerState viewer)
        {
            return new PlayerStateView
            {
                PlayerId = viewer.PlayerId,
                CharacterId = viewer.Character.CharacterId,
                HP = viewer.HP,
                Shield = viewer.Shield,
                Resources = viewer.Resources,
                HandSize = viewer.Hand.Count,
                Hand = ToViewList(viewer.Hand),
                DeckCount = viewer.Deck.Count,
                Deck = ToViewList(viewer.Deck),
                DiscardCount = viewer.Discard.Count,
                Discard = ToViewList(viewer.Discard),
                Board = viewer.Board,
                PassiveCount = viewer.Passives.Count,
                Passives = viewer.Passives,
                PastUpgrades = null,
                FutureOffers = viewer.UpcomingOffers,
                InsightTree = viewer.InsightTree,
                Silver = viewer.Silver,
                UpgradeChoiceSubmitted = viewer.UpgradeChoiceSubmitted,
                PowerCategoryCommitted = viewer.PowerCategoryCommitted,
                StrategyCategoryCommitted = viewer.StrategyCategoryCommitted
            };
        }

        private static List<BoardPermanent> PartialBoardView(List<BoardPermanent> board)
        {
            // Returns count-only — client knows how many but not what they are
            return board?.Select(_ => new BoardPermanent { PermanentId = "hidden" }).ToList();
        }

        private static PlayerStateView BuildOpponentView(PlayerState viewer)
        {
            var fog = viewer.FogReveals;
            var snap = viewer.TurnStartSnapshot;

            if (snap == null)
                return FullFogView();

            return new PlayerStateView
            {
                PlayerId = snap.PlayerID,

                CharacterId = fog.CharacterIdentity ? snap.CharacterId : null,
                HP = fog.CharacterHP ? snap.HP : -1,
                Shield = fog.CharacterHP ? snap.Shield : -1,
                Resources = fog.CharacterResources ? snap.Resources : null,

                HandSize = fog.HandSize ? snap.Hand.Count : -1,
                Hand = fog.HandContents ? ToViewList(snap.Hand) : null,

                DeckCount = fog.DrawPileCount ? snap.DeckCount : -1,

                Deck = fog.DrawPileContents ? ToViewList(snap.Deck) :
                fog.DrawPileOrdered ? ToViewList(snap.Deck) : null,

                DiscardCount = fog.DiscardPileCount ? snap.DiscardCount : -1,
                Discard = fog.DiscardPileContents ? ToViewList(snap.Discard) : null,

                Board = fog.BoardState ? snap.Board :
                fog.PermanentsOpponentCount ? PartialBoardView(snap.Board) : null,

                PassiveCount = fog.PassivesOpponentCount ? snap.PassiveCount : -1,
                Passives = fog.PassivesOpponent ? snap.Passives : null,

                PastUpgrades = fog.PastUpgrades ? GetPastUpgrades(viewer) : null,
                FutureOffers = fog.FutureUpgradesOpponent ? snap.UpcomingOffers : null,
                InsightTree = fog.InsightTreeOpponent ? GetOpponentInsightTree(viewer) : null,
                // Silver is not a revealable field — always hidden for opponent
                Silver = -1,
                UpgradeChoiceSubmitted = false,
                PowerCategoryCommitted = false,
                StrategyCategoryCommitted = false

            };
        }

        private static PlayerStateView FullFogView()
        {
            return new PlayerStateView
            {
                PlayerId = -1,
                CharacterId = null,
                HP = -1,
                Shield = -1,
                Resources = null,
                HandSize = -1,
                Hand = null,
                DeckCount = -1,
                Deck = null,
                DiscardCount = -1,
                Discard = null,
                Board = null,
                PassiveCount = -1,
                Passives = null,
                PastUpgrades = null,
                FutureOffers = null,
                InsightTree = null,
                Silver = -1,
                UpgradeChoiceSubmitted = false
            };
        }

        private static CardInstanceView ToView(CardInstance c)
        {
            return new CardInstanceView
            {
                InstanceId = c.InstanceId,
                CardId = c?.CardId,
                DisplayName = c?.DisplayName,
                ModifiedSpeed = c.ModifiedSpeed,
                Type = c?.Type ?? CardType.Queueable,
                Cost = c?.Cost ?? new ResourceCost()
            };
        }

        private static List<CardInstanceView> ToViewList(List<CardInstance> cards)
        {
            return cards?.Select(ToView).ToList();
        }

        private static List<QueueEntryView> BuildQueueView(List<QueueEntry> queue)
        {
            return queue.Select(e => new QueueEntryView
            {
                OwnerId = e.OwnerId,
                CurrentSpeed = e.CurrentSpeed,
                QueuePosition = e.QueuePosition,
                Card = ToView(e.Card)
            }).ToList();
        }

        // Phase 5 stubs
        private static List<string> GetPastUpgrades(PlayerState viewer) => null;
        private static InsightTreeState GetOpponentInsightTree(PlayerState viewer) => null;
    }
}
