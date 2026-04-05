using System;
using System.Collections.Generic;
using System.Linq;
using Mirror.BouncyCastle.Bcpg;
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
        public bool IsAttack;
        public string EffectId;
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

        // This player's shop offer for the current ShopPhase. Null outside ShopPhase.
        public ShopOffer OwnShopOffer;

        // Auction offer — visible to both players during AuctionPhase. Null outside.
        public AuctionOffer CurrentAuctionOffer;

        // Whether this player has already submitted their bids.
        public bool AuctionBidsSubmitted;
        // Current event during EventPhase. Null outside EventPhase.
        public string CurrentEventId;
        public string CurrentEventDisplayName;
        public string CurrentEventDescription;
        public string EventOutcome;
        public bool OwnChoiceSubmitted;    // has this player submitted their event choice
        public bool EventChoiceRequired;   // does this event require player input
        public List<CardInstanceView> EventRevealedCards;

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

        //List of status effects
        public List<StatusEffect> StatusEffects;

        public bool UpgradeChoiceSubmitted;
        public bool PowerCategoryCommitted;
        public bool StrategyCategoryCommitted;
        public bool InsightCategoryCommitted;
        public bool ShopDoneSubmitted;
    }

    // A queue entry as seen by a client.
    [Serializable]
    public class QueueEntryView
    {
        public int OwnerId;
        public int CurrentSpeed;
        public int QueuePosition;
        public CardInstanceView Card;
        public bool WasUpcast;
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // FOG FILTER
    // ─────────────────────────────────────────────────────────────────────────────

    public static class FogFilter
    {
        public static ClientGameStateView GenerateView(GameState state, int viewerPlayerId)
        {
            PlayerState viewer = state.GetPlayer(viewerPlayerId);

            bool viewerCommitted = state.CurrentPhase == TurnPhase.RoguelikePhase && viewer.UpgradeChoiceSubmitted
                || state.CurrentPhase == TurnPhase.ShopPhase && viewer.ShopDoneSubmitted
                || state.CurrentPhase == TurnPhase.AuctionPhase
                    && (viewerPlayerId == 0 ? state.AuctionOffer.Player0Submitted : state.AuctionOffer.Player1Submitted)
                || (state.CurrentPhase == TurnPhase.EventPhase
                    && state.PlayerEventChoices?[viewerPlayerId] != null);

            return new ClientGameStateView
            {
                CurrentPhase = state.CurrentPhase,
                TurnNumber = state.TurnNumber,
                OwnState = BuildOwnView(viewer),
                OpponentState = BuildOpponentView(viewer, state.GetOpponent(viewerPlayerId), state.CurrentPhase, viewerCommitted),
                OwnQueue = BuildQueueView(state.GetQueue(viewerPlayerId)),
                MergedQueue = state.MergedQueue != null
                    ? BuildQueueView(state.MergedQueue)
                    : null,
                GameOver = state.GameOver,
                WinnerPlayerId = state.WinnerPlayerId,
                OwnShopOffer = state.CurrentPhase == TurnPhase.ShopPhase
                    ? (viewerPlayerId == 0 ? state.Player0ShopOffer : state.Player1ShopOffer)
                    : null,
                CurrentAuctionOffer = state.CurrentPhase == TurnPhase.AuctionPhase
                    ? state.AuctionOffer
                    : null,
                AuctionBidsSubmitted = state.CurrentPhase == TurnPhase.AuctionPhase
                    ? (viewerPlayerId == 0 ? state.AuctionOffer.Player0Submitted : state.AuctionOffer.Player1Submitted)
                    : false,
                CurrentEventId = state.CurrentPhase == TurnPhase.EventPhase ? state.CurrentEventId : null,
                CurrentEventDisplayName = BuildEventDisplayName(state),
                CurrentEventDescription = BuildEventDescription(state),
                OwnChoiceSubmitted = state.CurrentPhase == TurnPhase.EventPhase
                    && state.PlayerEventChoices[viewerPlayerId] != null,
                EventChoiceRequired = BuildEventChoiceRequired(state),
                EventRevealedCards = BuildEventRevealedCards(state, viewerPlayerId),
                EventOutcome = state.CurrentPhase == TurnPhase.EventPhase ? state.EventOutcome : null
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
                InsightCategoryCommitted = viewer.InsightCategoryCommitted,
                StrategyCategoryCommitted = viewer.StrategyCategoryCommitted,
                ShopDoneSubmitted = viewer.ShopDoneSubmitted,
                StatusEffects = viewer.StatusEffects
            };
        }

        private static List<BoardPermanent> PartialBoardView(List<BoardPermanent> board)
        {
            // Returns count-only — client knows how many but not what they are
            return board?.Select(_ => new BoardPermanent { PermanentId = "hidden" }).ToList();
        }

        private static PlayerStateView BuildOpponentView(PlayerState viewer, PlayerState liveOpponent, TurnPhase phase, bool viewerCommitted)
        {
            var fog = viewer.FogReveals;
            var snap = viewer.TurnStartSnapshot;

            if (snap == null)
                return FullFogView();

            bool useLiveState = phase == TurnPhase.QueueResolution || viewerCommitted;

            return new PlayerStateView
            {
                PlayerId = snap.PlayerID,

                CharacterId = fog.CharacterIdentity ? snap.CharacterId : null,
                HP = fog.CharacterHP ? (useLiveState ? liveOpponent.HP : snap.HP) : -1,
                Shield = fog.CharacterHP ? (useLiveState ? liveOpponent.Shield : snap.Shield) : -1,
                Resources = fog.CharacterResources ? (useLiveState ? liveOpponent.Resources : snap.Resources) : null,

                HandSize = fog.HandSize ? (useLiveState ? liveOpponent.Hand.Count : snap.Hand.Count) : -1,
                Hand = fog.HandContents ? (useLiveState ? ToViewList(liveOpponent.Hand) : ToViewList(snap.Hand)) : null,

                DeckCount = fog.DrawPileCount ? (useLiveState ? liveOpponent.Deck.Count() : snap.DeckCount) : -1,

                Deck = fog.DrawPileContents ? (useLiveState ? ToViewList(liveOpponent.Deck) : ToViewList(snap.Deck)) :
                fog.DrawPileOrdered ? ToViewList(snap.Deck) : null,

                DiscardCount = fog.DiscardPileCount ? (useLiveState ? liveOpponent.Discard.Count() : snap.DiscardCount) : -1,
                Discard = fog.DiscardPileContents ? (useLiveState ? ToViewList(liveOpponent.Discard) : ToViewList(snap.Discard)) : null,

                Board = fog.BoardState ? (useLiveState ? liveOpponent.Board : snap.Board) :
                fog.PermanentsOpponentCount ? (useLiveState ? PartialBoardView(liveOpponent.Board) : PartialBoardView(snap.Board)) : null,

                PassiveCount = fog.PassivesOpponentCount ? (useLiveState ? liveOpponent.Passives.Count() : snap.PassiveCount) : -1,
                Passives = fog.PassivesOpponent ? (useLiveState ? liveOpponent.Passives : snap.Passives) : null,

                PastUpgrades = fog.PastUpgrades ? (useLiveState ? GetPastUpgrades(liveOpponent) : GetPastUpgrades(viewer)) : null,
                FutureOffers = fog.FutureUpgradesOpponent ? (useLiveState ? liveOpponent.UpcomingOffers : snap.UpcomingOffers) : null,
                InsightTree = fog.InsightTreeOpponent ? (useLiveState ? GetOpponentInsightTree(liveOpponent) : GetOpponentInsightTree(viewer)
                ) : fog.CharacterResources && snap.InsightTree != null
                        ? (useLiveState ? new InsightTreeState { SightBanked = liveOpponent.InsightTree.SightBanked }
                        : new InsightTreeState { SightBanked = snap.InsightTree.SightBanked })
                        : null,
                Silver = fog.CharacterResources ? (useLiveState ? liveOpponent.Silver : snap.Silver) : -1,
                UpgradeChoiceSubmitted = false,
                PowerCategoryCommitted = false,
                InsightCategoryCommitted = false,
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
                UpgradeChoiceSubmitted = false,
                InsightCategoryCommitted = false,
                PowerCategoryCommitted = false,
                StrategyCategoryCommitted = false
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
                Cost = c?.Cost ?? new ResourceCost(),
                IsAttack = c.IsAttack,
                EffectId = c?.EffectId
            };
        }

        public static CardInstanceView ToViewPublic(CardInstance c) => ToView(c);

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
                Card = ToView(e.Card),
                WasUpcast = e.Card.WasUpcast
            }).ToList();
        }

        // Phase 5 stubs
        private static List<string> GetPastUpgrades(PlayerState viewer) => null;
        private static InsightTreeState GetOpponentInsightTree(PlayerState viewer)
        {
            return viewer.TurnStartSnapshot?.InsightTree?.Clone();
        }
        private static string BuildEventDisplayName(GameState state)
        {
            if (state.CurrentPhase != TurnPhase.EventPhase || state.CurrentEventId == null) return null;
            return state.CurrentEventId;
        }

        private static string BuildEventDescription(GameState state)
        {
            // Description is looked up client-side from eventId
            return null;
        }

        private static bool BuildEventChoiceRequired(GameState state)
        {
            if (state.CurrentPhase != TurnPhase.EventPhase) return false;
            var effect = GameEventRegistry.Instance?.GetEffect(state.CurrentEventId);
            return effect?.IsInteractive ?? false;
        }

        private static List<CardInstanceView> BuildEventRevealedCards(GameState state, int viewerPlayerId)
        {
            if (state.CurrentPhase != TurnPhase.EventPhase) return null;
            if (state.CurrentEventId != "writing_on_the_wall") return null;
            var player = state.GetPlayer(viewerPlayerId);
            // Server stores revealed cards in a temp list on GameState
            return state.EventRevealedCards?[viewerPlayerId]?
                .Select(c => FogFilter.ToViewPublic(c)).ToList();
        }
    }
}