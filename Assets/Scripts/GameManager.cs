using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using FogClouds;
using System.Linq;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    private GameState _gameState;
    private readonly List<PlayerNetworkAgent> _registeredPlayers = new();

    [SyncVar]
    public TurnPhase CurrentPhase = TurnPhase.TurnStart;

    private const float MainPhaseTimerDuration = 60f;
    private int _endTurnVotes = 0;
    private int _roguelikeVotes = 0;

    private int _shopVotes = 0;

    private bool IsShopTurn() => _gameState.TurnNumber % 4 == 0 && _gameState.TurnNumber % 8 != 0;
    private bool IsAuctionTurn() => _gameState.TurnNumber % 8 == 0;

    private bool IsEventTurn() => _gameState.TurnNumber % 2 == 0 && !IsShopTurn() && !IsAuctionTurn();

    [SerializeField] private InsightTreeDefinition _insightTree;
    [SerializeField] private CardPoolRegistry _cardPool;
    [SerializeField] private GameEventDefinition _eventPool;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        GameState.SetLogger(msg => Debug.Log(msg));
    }

    //Registration

    [Server]
    public void RegisterPlayer(PlayerNetworkAgent agent)
    {
        if (_registeredPlayers.Contains(agent)) return;

        _registeredPlayers.Add(agent);
        Debug.Log($"[GameManager] Player registered. Total: {_registeredPlayers.Count}");

        if (_registeredPlayers.Count == 2)
            InitializeGame();
    }

    [Server]
    private void InitializeGame()
    {
        var p0Character = Resources.Load<CharacterData>("Characters/thessa");
        var p1Character = Resources.Load<CharacterData>("Characters/thessa");

        if (p0Character == null || p1Character == null)
        {
            Debug.LogError("[GameManager] Could not load CharacterData assets.");
            return;
        }

        _gameState = new GameState(p0Character, p1Character);
        Debug.Log("[GameManager] GameState initialized. Both players registered.");

        StateRelay.Instance.Initialize(_gameState, _registeredPlayers);
        Debug.Log("[GameManager] StateRelay initialized.");

        BuildStartingDeck(_gameState.GetPlayer(0));
        BuildStartingDeck(_gameState.GetPlayer(1));

        _gameState.GetPlayer(0).DrawCards(5, _gameState.Rng);
        _gameState.GetPlayer(1).DrawCards(5, _gameState.Rng);

        AdvancePhase();
    }

    //Phase State Machine
    [Server]
    private void AdvancePhase()
    {
        switch (CurrentPhase)
        {
            case TurnPhase.TurnStart:
                EnterMainPhase();
                break;
            case TurnPhase.MainPhase:
                EnterQueueMerge();
                break;
            case TurnPhase.QueueMerge:
                EnterQueueResolution();
                break;
            case TurnPhase.QueueResolution:
                EnterRoguelikePhase();
                break;
            case TurnPhase.RoguelikePhase:
                if (IsShopTurn()) EnterShopPhase();
                else if (IsAuctionTurn()) EnterAuctionPhase();
                else if (IsEventTurn()) EnterEventPhase();
                else EnterTurnEnd();
                break;
            case TurnPhase.ShopPhase:
                EnterTurnEnd();
                break;
            case TurnPhase.AuctionPhase:
                EnterTurnEnd();
                break;
            case TurnPhase.EventPhase:
                EnterTurnEnd();
                break;
            case TurnPhase.TurnEnd:
                EnterTurnStart();
                break;
        }
    }

    [Server]
    private void EnterTurnStart()
    {
        CurrentPhase = TurnPhase.TurnStart;
        _gameState.CurrentPhase = TurnPhase.TurnStart;
        Debug.Log($"[GameManager] Phase: TurnStart (Turn {_gameState.TurnNumber})");

        var p0 = _gameState.GetPlayer(0);
        var p1 = _gameState.GetPlayer(1);

        p0.OnTurnStart();
        p1.OnTurnStart();

        //Apply status effects
        ApplyTurnStartStatusEffects(_gameState.GetPlayer(0));
        ApplyTurnStartStatusEffects(_gameState.GetPlayer(1));

        //Apply passive effects
        ApplyPassiveTurnStart(_gameState.GetPlayer(0));
        ApplyPassiveTurnStart(_gameState.GetPlayer(1));

        // Tick permanents and remove expired ones
        TickPermanents(p0);
        TickPermanents(p1);

        // Draw cards
        p0.DrawCards(5, _gameState.Rng);
        p1.DrawCards(5, _gameState.Rng);

        AwardSilver(p0, 5, "turn start");
        AwardSilver(p1, 5, "turn start");

        AdvancePhase();
    }

    [Server]
    private void EnterMainPhase()
    {
        CurrentPhase = TurnPhase.MainPhase;
        _gameState.CurrentPhase = TurnPhase.MainPhase;
        _endTurnVotes = 0;
        Debug.Log("[GameManager] Phase: MainPhase — timer started.");

        _gameState.TakeSnapshots();
        StateRelay.Instance.BroadcastToAll();
    }

    [Server]
    private void EnterQueueMerge()
    {
        CurrentPhase = TurnPhase.QueueMerge;
        _gameState.CurrentPhase = TurnPhase.QueueMerge;
        Debug.Log("[GameManager] Phase: QueueMerge.");
        _gameState.MergeQueues();
        StateRelay.Instance.BroadcastToAll();
        AdvancePhase();
    }

    [Server]
    private void EnterQueueResolution()
    {
        CurrentPhase = TurnPhase.QueueResolution;
        _gameState.CurrentPhase = TurnPhase.QueueResolution;
        Debug.Log($"[GameManager] Phase: QueueResolution — resolving {_gameState.MergedQueue.Count} cards.");
        StateRelay.Instance.BroadcastToAll();
        StartCoroutine(ResolveQueue());
    }

    [Server]
    private void EnterRoguelikePhase()
    {
        CurrentPhase = TurnPhase.RoguelikePhase;
        _gameState.CurrentPhase = TurnPhase.RoguelikePhase;
        _roguelikeVotes = 0;
        Debug.Log("[GameManager] Phase: RoguelikePhase — generating offers.");

        GenerateRoguelikeOffers(_gameState.GetPlayer(0));
        GenerateRoguelikeOffers(_gameState.GetPlayer(1));

        //Snapshot taken at beginning of roguelike phase so opponent info doesn't regress back to TurnStartSnapshot after QueueResolution ends

        _gameState.Player1.TurnStartSnapshot = PlayerSnapshot.From(_gameState.Player2);
        _gameState.Player2.TurnStartSnapshot = PlayerSnapshot.From(_gameState.Player1);

        StateRelay.Instance.BroadcastToAll();
    }

    [Server]
    private void EnterShopPhase()
    {
        CurrentPhase = TurnPhase.ShopPhase;
        _gameState.CurrentPhase = TurnPhase.ShopPhase;
        Debug.Log("[GameManager] Phase: ShopPhase — generating offers.");

        _gameState.Player0ShopOffer = GenerateShopOffer(_gameState.GetPlayer(0));
        _gameState.Player1ShopOffer = GenerateShopOffer(_gameState.GetPlayer(1));

        _gameState.Player1.TurnStartSnapshot = PlayerSnapshot.From(_gameState.Player2);
        _gameState.Player2.TurnStartSnapshot = PlayerSnapshot.From(_gameState.Player1);

        _shopVotes = 0;
        StateRelay.Instance.BroadcastToAll();
    }

    [Server]
    private void EnterAuctionPhase()
    {
        CurrentPhase = TurnPhase.AuctionPhase;
        _gameState.CurrentPhase = TurnPhase.AuctionPhase;
        Debug.Log("[GameManager] Phase: AuctionPhase — generating cards.");

        _gameState.AuctionOffer.Reset();

        var auctionPicks = PickRandom(_cardPool.Auction, 3);

        _gameState.Player1.TurnStartSnapshot = PlayerSnapshot.From(_gameState.Player2);
        _gameState.Player2.TurnStartSnapshot = PlayerSnapshot.From(_gameState.Player1);

        foreach (var card in auctionPicks)
        {
            _gameState.AuctionOffer.CardIds.Add(card.CardId);
            _gameState.AuctionOffer.CardDisplayNames.Add(card.DisplayName);
        }


        StateRelay.Instance.BroadcastToAll();
    }

    [Server]
    private void EnterEventPhase()
    {
        CurrentPhase = TurnPhase.EventPhase;
        _gameState.CurrentPhase = TurnPhase.EventPhase;

        _gameState.Player1.TurnStartSnapshot = PlayerSnapshot.From(_gameState.Player2);
        _gameState.Player2.TurnStartSnapshot = PlayerSnapshot.From(_gameState.Player1);

        _gameState.PlayerEventChoices = new string[2];

        if (_eventPool == null || _eventPool.EventPool == null || _eventPool.EventPool.Length == 0)
        {
            Debug.LogWarning("[GameManager] No event pool assigned — skipping EventPhase.");
            AdvancePhase();
            return;
        }

        // Pick random event
        int pick = _gameState.Rng.Next(_eventPool.EventPool.Length);
        var gameEvent = _eventPool.EventPool[pick];
        _gameState.CurrentEventId = gameEvent.EventId;

        Debug.Log($"[GameManager] Phase: EventPhase — event: {gameEvent.DisplayName}");

        var effect = GameEventRegistry.Instance.GetEffect(_gameState.CurrentEventId);
        if (effect == null)
        {
            Debug.LogWarning($"[GameManager] No effect registered for event: {_gameState.CurrentEventId} — skipping.");
            AdvancePhase();
            return;
        }

        if (!effect.IsInteractive)
        {
            effect.Apply(_gameState);
            Debug.Log($"[GameManager] Event {_gameState.CurrentEventId} applied immediately.");
            StateRelay.Instance.BroadcastToAll();
            AdvancePhase();
        }
        else
        {
            // Writing on the Wall needs server-side setup before players can choose
            if (_gameState.CurrentEventId == "writing_on_the_wall")
                effect.Apply(_gameState);

            StateRelay.Instance.BroadcastToAll();
        }
    }

    [Server]
    private void EnterTurnEnd()
    {
        CurrentPhase = TurnPhase.TurnEnd;
        _gameState.CurrentPhase = TurnPhase.TurnEnd;
        Debug.Log("[GameManager] Phase: TurnEnd.");

        var p0 = _gameState.GetPlayer(0);
        var p1 = _gameState.GetPlayer(1);
        p0.Discard.AddRange(p0.Hand);
        p0.Hand.Clear();
        p1.Discard.AddRange(p1.Hand);
        p1.Hand.Clear();
        Debug.Log("[GameManager] Both players discarded their hands.");

        _gameState.TurnNumber++;
        _gameState.CheckWinCondition();

        if (_gameState.GameOver)
        {
            if (_gameState.GameOver)
            {
                RevealAllFog();
                Debug.Log($"[GameManager] Game over. Winner: Player {_gameState.WinnerPlayerId}");
                StateRelay.Instance.BroadcastToAll();
                // await rematch votes
                return;
            }
        }

        AdvancePhase();
    }

    //End Turn Intent

    [Server]
    public void HandleEndTurnIntent(PlayerNetworkAgent agent)
    {
        if (CurrentPhase != TurnPhase.MainPhase)
        {
            Debug.LogWarning("[GameManager] EndTurn intent received outside MainPhase — ignored.");
            return;
        }

        int playerId = _registeredPlayers.IndexOf(agent);
        _gameState.GetPlayer(playerId).ReadyToEndTurn = true;
        _endTurnVotes++;
        Debug.Log($"[GameManager] Player {playerId} submitted EndTurn. Votes: {_endTurnVotes}/2");

        if (_endTurnVotes >= 2)
        {
            Debug.Log("[GameManager] Both players ready — advancing early.");
            StopAllCoroutines();
            AdvancePhase();
        }
    }

    //Queue Operations
    [Server]
    public void HandleQueueCard(PlayerNetworkAgent agent, int cardInstanceId, bool upcast = false)
    {
        if (CurrentPhase != TurnPhase.MainPhase)
        {
            Debug.LogWarning($"[GameManager] HandleQueueCard called outside MainPhase — ignored.");
            return;
        }

        int playerId = _registeredPlayers.IndexOf(agent);
        var player = _gameState.GetPlayer(playerId);

        var card = player.Hand.Find(c => c.InstanceId == cardInstanceId);
        if (card == null)
        {
            Debug.LogWarning($"[GameManager] Player {playerId} tried to queue card {cardInstanceId} but it wasn't in their hand.");
            return;
        }

        if (card.Type != CardType.Queueable)
        {
            Debug.LogWarning($"[GameManager] Player {playerId} tried to queue non-queueable card {card}.");
            return;
        }

        if (!CanAfford(player, card.Cost))
        {
            Debug.LogWarning($"[GameManager] Player {playerId} cannot afford {card}.");
            return;
        }

        string effectId = card.EffectId;
        if (upcast)
        {
            if (!card.IsAttack)
            {
                Debug.LogWarning($"[GameManager] Player {playerId} tried to upcast non-attack card {card}.");
                return;
            }

            if (player.Resources.PerTurnResource < 1)
            {
                Debug.LogWarning($"[GameManager] Player {playerId} tried to upcast but has no Daggers.");
                return;
            }

            string upcastId = effectId.Replace("_base", "") + "_upcast";
            if (CardEffectRegistry.Instance.GetEffect(upcastId) == null)
            {
                Debug.LogWarning($"[GameManager] Card {effectId} has no upcast variant.");
                return;
            }

            player.Resources.PerTurnResource -= 1;
            effectId = upcastId;
        }

        DeductCost(player, card.Cost);
        player.Hand.Remove(card);
        player.Discard.Add(card);

        _gameState.EnqueueCard(playerId, card, wasUpcast: upcast);

        AwardSilver(player, 2, "card queued");

        StateRelay.Instance.BroadcastToAll();

        Debug.Log($"[GameManager] Player {playerId} queued {card} (upcast: {upcast}). Queue size: {_gameState.GetQueue(playerId).Count}");

    }

    [Server]
    public void HandleQueueFirstCard(PlayerNetworkAgent agent, bool upcast = false)
    {
        int playerId = _registeredPlayers.IndexOf(agent);
        var player = _gameState.GetPlayer(playerId);

        if (player.Hand.Count == 0)
        {
            Debug.LogWarning($"[GameManager] Player {playerId} has no cards in hand.");
            return;
        }

        HandleQueueCard(agent, player.Hand[0].InstanceId, upcast);
    }

    [Server]
    private IEnumerator ResolveQueue()
    {
        while (_gameState.MergedQueue.Count > 0)
        {
            StateRelay.Instance.BroadcastToAll();
            yield return new WaitForSeconds(1.2f);

            var entry = _gameState.MergedQueue[0];
            _gameState.MergedQueue.RemoveAt(0);

            Debug.Log($"[GameManager] Resolving: {entry.Card} (owner: Player {entry.OwnerId}, speed: {entry.CurrentSpeed})");

            string resolveEffectId = entry.WasUpcast
                ? entry.Card.EffectId.Replace("_base", "") + "_upcast"
                : entry.Card.EffectId;
            var effect = CardEffectRegistry.Instance.GetEffect(resolveEffectId);
            effect?.Apply(entry, _gameState);

            // Totem of Progress — draw 1 if card was upcast
            if (entry.WasUpcast)
            {
                var caster = _gameState.GetPlayer(entry.OwnerId);
                if (caster.Board.Any(p => p.PermanentId == "totem_of_progress"))
                {
                    caster.DrawCards(1, _gameState.Rng);
                    Debug.Log($"[GameManager] Totem of Progress triggered — Player {entry.OwnerId} drew 1 card.");
                }
            }

            StateRelay.Instance.BroadcastToAll();
            yield return new WaitForSeconds(0.8f);

            // Check for game over after every resolution — don't allow subsequent cards to undo a kill
            if (_gameState.GameOver)
            {
                Debug.Log("[GameManager] Game over detected mid-resolution — stopping queue.");
                RevealAllFog();
                StateRelay.Instance.BroadcastToAll();
                yield break;
            }
        }

        Debug.Log("[GameManager] Queue resolution complete.");
        AdvancePhase();
    }

    [Server]
    private void BuildStartingDeck(PlayerState player)
    {
        var deckList = new List<(string cardId, int copies)>
        {
            ("KnifeStrikeBase",  4),
            ("Cultellara",       1),
            ("Brace",            4),
            ("ChaChaCard",       1),
            ("Cultivita",        1),
        };

        int instanceId = player.PlayerId * 100;
        foreach (var (cardId, copies) in deckList)
        {
            var definition = Resources.Load<CardDefinition>($"Cards/{cardId}");
            if (definition == null)
            {
                Debug.LogWarning($"[GameManager] Could not load CardDefinition: Cards/{cardId}");
                continue;
            }

            for (int i = 0; i < copies; i++)
                player.Deck.Add(new CardInstance(definition, instanceId++));
        }

        player.ShuffleDeck(_gameState.Rng);
        Debug.Log($"[GameManager] Built starting deck for Player {player.PlayerId}. {player.Deck.Count} cards.");
    }

    [Server]
    private void TickPermanents(PlayerState player)
    {
        for (int i = player.Board.Count - 1; i >= 0; i--)
        {
            var permanent = player.Board[i];
            permanent.OnTurnStart(); // clear ProtectedThisTurn, any other per-turn resets
            if (permanent.TurnsRemaining == -1) continue;
            permanent.TurnsRemaining--;
            if (permanent.TurnsRemaining <= 0)
            {
                Debug.Log($"[GameManager] {permanent.DisplayName} expired for Player {player.PlayerId}.");
                if (permanent.SourceCard != null) { player.Discard.Add(permanent.SourceCard); }
                player.Board.RemoveAt(i);
            }
        }
    }

    [Server]
    public void HandlePlayInstant(PlayerNetworkAgent agent, int cardInstanceId)
    {
        int playerId = _registeredPlayers.IndexOf(agent);
        var player = _gameState.GetPlayer(playerId);

        if (CurrentPhase != TurnPhase.MainPhase)
        {
            Debug.LogWarning($"[GameManager] Player {playerId} tried to play an instant outside MainPhase.");
            return;
        }

        var card = player.Hand.Find(c => c.InstanceId == cardInstanceId);
        if (card == null)
        {
            Debug.LogWarning($"[GameManager] Player {playerId} tried to play instant {cardInstanceId} but it wasn't in their hand.");
            return;
        }

        if (card.Type != CardType.Instant)
        {
            Debug.LogWarning($"[GameManager] Player {playerId} tried to play non-instant {card} as instant.");
            return;
        }

        if (!CanAfford(player, card.Cost))
        {
            Debug.LogWarning($"[GameManager] Player {playerId} cannot afford {card}.");
            return;
        }

        DeductCost(player, card.Cost);
        player.Hand.Remove(card);
        player.Discard.Add(card);

        var effect = CardEffectRegistry.Instance.GetEffect(card.EffectId);
        effect?.Apply(new QueueEntry(playerId, card, 0), _gameState);

        // Mirror of Moonlight — queue a copy of this instant at speed 2
        if (player.MirrorActive)
        {
            var mirrorEntry = new QueueEntry(playerId, card, 2);
            _gameState.GetQueue(playerId).Add(mirrorEntry);
            Debug.Log($"[GameManager] Mirror copied {card} into queue at speed 2.");
        }

        AwardSilver(player, 2, "instant played");

        StateRelay.Instance.BroadcastToAll();
        Debug.Log($"[GameManager] Player {playerId} played instant {card}.");
    }

    [Server]
    public void HandlePlayPermanent(PlayerNetworkAgent agent, int cardInstanceId)
    {
        int playerId = _registeredPlayers.IndexOf(agent);
        var player = _gameState.GetPlayer(playerId);

        if (CurrentPhase != TurnPhase.MainPhase)
        {
            Debug.LogWarning($"[GameManager] Player {playerId} tried to play a permanent outside MainPhase.");
            return;
        }

        var card = player.Hand.Find(c => c.InstanceId == cardInstanceId);
        if (card == null)
        {
            Debug.LogWarning($"[GameManager] Player {playerId} tried to play permanent {cardInstanceId} but it wasn't in their hand.");
            return;
        }

        if (card.Type != CardType.Permanent)
        {
            Debug.LogWarning($"[GameManager] Player {playerId} tried to play non-permanent {card} as permanent.");
            return;
        }

        if (!CanAfford(player, card.Cost))
        {
            Debug.LogWarning($"[GameManager] Player {playerId} cannot afford {card}.");
            return;
        }

        DeductCost(player, card.Cost);
        player.Hand.Remove(card);
        // Permanents are removed from the game entirely — not added to discard

        var effect = CardEffectRegistry.Instance.GetEffect(card.EffectId);
        effect?.Apply(new QueueEntry(playerId, card, 0), _gameState);

        AwardSilver(player, 2, "permanent played");

        StateRelay.Instance.BroadcastToAll();
        Debug.Log($"[GameManager] Player {playerId} played permanent {card}.");
    }

    [Server]
    public void HandlePlayFirstPermanent(PlayerNetworkAgent agent)
    {
        int playerId = _registeredPlayers.IndexOf(agent);
        var player = _gameState.GetPlayer(playerId);

        var permanent = player.Hand.Find(c => c.Type == CardType.Permanent);
        if (permanent == null)
        {
            Debug.LogWarning($"[GameManager] Player {playerId} has no permanents in hand.");
            return;
        }

        HandlePlayPermanent(agent, permanent.InstanceId);
    }

    [Server]
    private bool CanAfford(PlayerState player, ResourceCost cost)
    {
        if (player.Resources.PerTurnResource < cost.Daggers) return false;
        if (player.HP <= cost.Blood) return false; // must survive the cost
        return true;
    }

    [Server]
    private void DeductCost(PlayerState player, ResourceCost cost)
    {
        player.Resources.PerTurnResource -= cost.Daggers;
        player.DaggersSpentThisTurn += cost.Daggers;

        // Totem of Sacrifice reduces blood costs by 1 per stack, minimum 1
        int bloodCost = cost.Blood;
        if (bloodCost > 0)
        {
            int reduction = 0;
            foreach (var permanent in player.Board)
                if (permanent.PermanentId == "totem_of_sacrifice")
                    reduction++;
            bloodCost = Mathf.Max(1, bloodCost - reduction);
        }

        player.HP -= bloodCost;
        player.HP = Mathf.Max(player.HP, 0);
        player.BloodSpentThisTurn += bloodCost;
        Debug.Log($"[GameManager] Player {player.PlayerId} paid {cost.Daggers} Daggers, {bloodCost} Blood.");
        _gameState.CheckWinCondition();
    }

    [Server]
    public void HandlePlayFirstInstant(PlayerNetworkAgent agent)
    {
        int playerId = _registeredPlayers.IndexOf(agent);
        var player = _gameState.GetPlayer(playerId);

        var instant = player.Hand.Find(c => c.Type == CardType.Instant);
        if (instant == null)
        {
            Debug.LogWarning($"[GameManager] Player {playerId} has no instants in hand.");
            return;
        }

        HandlePlayInstant(agent, instant.InstanceId);
    }

    [Server]
    private void ApplyFogReveal(PlayerState player, string flagName)
    {
        switch (flagName)
        {
            case "CharacterIdentity": player.FogReveals.CharacterIdentity = true; break;
            case "CharacterHP": player.FogReveals.CharacterHP = true; break;
            case "CharacterResources": player.FogReveals.CharacterResources = true; break;
            case "HandSize": player.FogReveals.HandSize = true; break;
            case "HandContents": player.FogReveals.HandContents = true; break;
            case "DrawPileCount": player.FogReveals.DrawPileCount = true; break;
            case "DrawPileContents": player.FogReveals.DrawPileContents = true; break;
            case "DiscardPileCount": player.FogReveals.DiscardPileCount = true; break;
            case "DiscardPileContents": player.FogReveals.DiscardPileContents = true; break;
            case "BoardState": player.FogReveals.BoardState = true; break;
            case "PassivesOpponentCount": player.FogReveals.PassivesOpponentCount = true; break;
            case "PassivesOpponent": player.FogReveals.PassivesOpponent = true; break;
            case "PastUpgrades": player.FogReveals.PastUpgrades = true; break;
            case "FutureUpgradesOpponent": player.FogReveals.FutureUpgradesOpponent = true; break;
            case "FutureUpgradesSelf": player.FogReveals.FutureUpgradesSelf = true; break;
            case "InsightTreeOpponent": player.FogReveals.InsightTreeOpponent = true; break;
            case "PermanentsOpponentCount": player.FogReveals.PermanentsOpponentCount = true; break;
            case "DrawPileOrdered": player.FogReveals.DrawPileOrdered = true; break;
            default:
                Debug.LogWarning($"[GameManager] Unknown fog flag: {flagName}");
                break;
        }
    }

    [Server]
    public void HandleUnlockNode(PlayerNetworkAgent agent, string nodeId)
    {
        int playerId = _registeredPlayers.IndexOf(agent);
        var player = _gameState.GetPlayer(playerId);
        var node = _insightTree.AllNodes.FirstOrDefault(n => n.NodeId == nodeId);

        if (node == null)
        {
            Debug.LogWarning($"[GameManager] Unknown node: {nodeId}");
            return;
        }

        if (player.InsightTree.SightBanked < node.Cost)
        {
            Debug.LogWarning($"[GameManager] Player {playerId} cannot afford node {nodeId}.");
            return;
        }

        // Check prerequisites
        foreach (var prereq in node.Prerequisites)
        {
            if (!player.InsightTree.IsUnlocked(prereq.NodeId))
            {
                Debug.LogWarning($"[GameManager] Player {playerId} has not unlocked prerequisite {prereq.NodeId}.");
                return;
            }
        }

        player.InsightTree.SightBanked -= node.Cost;
        player.InsightTree.Unlock(nodeId);
        ApplyFogReveal(player, node.FlagName);

        Debug.Log($"[GameManager] Player {playerId} unlocked {node.DisplayName}. Sight remaining: {player.InsightTree.SightBanked}");
        StateRelay.Instance.BroadcastToAll();
    }

    [Server]
    public void HandleGrantSight(PlayerNetworkAgent agent, int amount = 1)
    {
        int playerId = _registeredPlayers.IndexOf(agent);
        var player = _gameState.GetPlayer(playerId);
        player.InsightTree.SightBanked += amount;
        Debug.Log($"[GameManager] Player {playerId} gained {amount} Sight. Total: {player.InsightTree.SightBanked}");
    }

    [Server]
    public void HandleRoguelikeChoice(PlayerNetworkAgent agent, RoguelikeCategory category, string selectionId)
    {
        if (CurrentPhase != TurnPhase.RoguelikePhase)
        {
            Debug.LogWarning("[GameManager] Roguelike choice received outside RoguelikePhase — ignored.");
            return;
        }

        int playerId = _registeredPlayers.IndexOf(agent);
        var player = _gameState.GetPlayer(playerId);

        if (player.UpgradeChoiceSubmitted)
        {
            Debug.LogWarning($"[GameManager] Player {playerId} already submitted a roguelike choice.");
            return;
        }

        switch (category)
        {
            case RoguelikeCategory.Insight:
                HandleGrantSight(agent);
                // selectionId is the nodeId to unlock, if any
                if (!string.IsNullOrEmpty(selectionId))
                    HandleUnlockNode(agent, selectionId);
                break;
            case RoguelikeCategory.Power:
                Debug.Log($"[GameManager] Player {playerId} chose {category} upgrade: {selectionId} [Reached Power tab in the one switch statement");
                break;
            case RoguelikeCategory.Strategy:
                Debug.Log($"[GameManager] Player {playerId} chose {category} upgrade: {selectionId} [Reached Strategy tab in the one switch statement].");
                break;
        }

        player.UpgradeChoiceSubmitted = true;
        _roguelikeVotes++;
        Debug.Log($"[GameManager] Roguelike votes: {_roguelikeVotes}/2");

        if (_roguelikeVotes >= 2)
        {
            Debug.Log("[GameManager] Both players submitted roguelike choices — advancing.");
            AdvancePhase();
        }
    }

    [Server]
    public void HandleChooseInsight(PlayerNetworkAgent agent)
    {
        int playerId = _registeredPlayers.IndexOf(agent);
        var player = _gameState.GetPlayer(playerId);

        if (CurrentPhase != TurnPhase.RoguelikePhase)
        {
            Debug.LogWarning("[GameManager] INSIGHT choice received outside RoguelikePhase.");
            return;
        }

        if (player.UpgradeChoiceSubmitted || player.InsightCategoryCommitted)
        {
            Debug.LogWarning($"[GameManager] Player {playerId} already committed to INSIGHT.");
            return;
        }

        player.InsightCategoryCommitted = true;
        HandleGrantSight(agent);
        Debug.Log($"[GameManager] Player {playerId} committed to INSIGHT. Sight: {player.InsightTree.SightBanked}");
        StateRelay.Instance.BroadcastToAll();
    }

    [Server]
    public void HandleSubmitRoguelikeChoice(PlayerNetworkAgent agent)
    {
        int playerId = _registeredPlayers.IndexOf(agent);
        var player = _gameState.GetPlayer(playerId);

        if (CurrentPhase != TurnPhase.RoguelikePhase)
        {
            Debug.LogWarning("[GameManager] Roguelike submit received outside RoguelikePhase.");
            return;
        }

        if (player.UpgradeChoiceSubmitted)
        {
            Debug.LogWarning($"[GameManager] Player {playerId} already submitted.");
            return;
        }

        player.UpgradeChoiceSubmitted = true;
        _roguelikeVotes++;
        Debug.Log($"[GameManager] Player {playerId} submitted roguelike choice. Votes: {_roguelikeVotes}/2");

        if (_roguelikeVotes >= 2)
        {
            Debug.Log("[GameManager] Both players submitted — advancing.");
            AdvancePhase();
        }
    }

    [Server]
    private void ResolveAuction()
    {
        var auction = _gameState.AuctionOffer;
        for (int i = 0; i < auction.CardIds.Count; i++)
        {
            int p0Bid = auction.Player0Bids[i];
            int p1Bid = auction.Player1Bids[i];

            if (p0Bid > p1Bid)
                AwardAuctionCard(0, auction.CardIds[i], p0Bid);
            else if (p1Bid > p0Bid)
                AwardAuctionCard(1, auction.CardIds[i], p1Bid);
            else
                Debug.Log($"[GameManager] Auction tie on {auction.CardIds[i]} — no winner.");
        }
    }

    [Server]
    private void AwardAuctionCard(int playerId, string cardId, int silverCost)
    {
        var player = _gameState.GetPlayer(playerId);
        player.Silver -= silverCost;
        AddCardToDeck(player, cardId);
        Debug.Log($"[GameManager] Player {playerId} won auction for {cardId}, paid {silverCost} Silver. Silver remaining: {player.Silver}");
    }

    [Server]
    public void HandleEventChoice(PlayerNetworkAgent agent, string choice)
    {
        if (CurrentPhase != TurnPhase.EventPhase)
        {
            Debug.LogWarning("[GameManager] Event choice received outside EventPhase — ignored.");
            return;
        }

        int playerId = _registeredPlayers.IndexOf(agent);

        if (_gameState.PlayerEventChoices[playerId] != null)
        {
            Debug.LogWarning($"[GameManager] Player {playerId} already submitted event choice.");
            return;
        }

        var effect = GameEventRegistry.Instance.GetEffect(_gameState.CurrentEventId);
        if (effect == null)
        {
            Debug.LogWarning($"[GameManager] No effect for current event: {_gameState.CurrentEventId}");
            return;
        }

        if (!effect.ValidateChoice(_gameState, playerId, choice, out string reason))
        {
            Debug.LogWarning($"[GameManager] Player {playerId} event choice rejected: {reason}");
            return;
        }

        effect.ApplyChoice(_gameState, playerId, choice);
        _gameState.PlayerEventChoices[playerId] = choice;

        Debug.Log($"[GameManager] Player {playerId} submitted event choice: {choice}");

        StateRelay.Instance.BroadcastToAll();

        if (effect.IsResolved(_gameState))
        {
            effect.Apply(_gameState);
            Debug.Log($"[GameManager] Event {_gameState.CurrentEventId} resolved.");
            StateRelay.Instance.BroadcastToAll();
            AdvancePhase();
        }
    }

    [Server]
    private void GenerateRoguelikeOffers(PlayerState player)
    {
        var pool = _cardPool;
        var characterId = player.Character.CharacterId;
        var offers = player.UpcomingOffers;

        offers.PowerOffers.Clear();
        offers.PowerPrices.Clear();
        offers.PowerDisplayNames.Clear();
        offers.StrategyOffers.Clear();
        offers.StrategyPrices.Clear();
        offers.StrategyDisplayNames.Clear();

        var powerPool = pool.GetPowerPool(characterId);
        var strategyPool = pool.GetStrategyPool(characterId);

        // Pick 3 random non-duplicate cards from each pool
        var powerPicks = PickRandom(powerPool, 3);
        var strategyPicks = PickRandom(strategyPool, 3);

        foreach (var card in powerPicks)
        {
            offers.PowerOffers.Add(card.CardId);
            offers.PowerDisplayNames.Add(card.DisplayName);
            offers.PowerPrices.Add(ShopPricing.Roll(_gameState.Rng, ShopPricing.PowerCard));
        }

        foreach (var card in strategyPicks)
        {
            offers.StrategyOffers.Add(card.CardId);
            offers.StrategyDisplayNames.Add(card.DisplayName);
            offers.StrategyPrices.Add(ShopPricing.Roll(_gameState.Rng, ShopPricing.StrategyCard));
        }
    }

    [Server]
    private List<CardDefinition> PickRandom(CardDefinition[] pool, int count)
    {
        var result = new List<CardDefinition>();
        if (pool == null || pool.Length == 0) return result;

        var indices = new List<int>();
        for (int i = 0; i < pool.Length; i++) indices.Add(i);

        count = Mathf.Min(count, pool.Length);
        for (int i = 0; i < count; i++)
        {
            int pick = _gameState.Rng.Next(indices.Count);
            result.Add(pool[indices[pick]]);
            indices.RemoveAt(pick);
        }
        return result;
    }

    [Server]
    public void HandleChoosePower(PlayerNetworkAgent agent, int offerIndex)
    {
        int playerId = _registeredPlayers.IndexOf(agent);
        var player = _gameState.GetPlayer(playerId);

        if (CurrentPhase != TurnPhase.RoguelikePhase)
        {
            Debug.LogWarning($"[GameManager] Player {playerId} tried to choose Power outside RoguelikePhase.");
            return;
        }

        if (player.UpgradeChoiceSubmitted)
        {
            Debug.LogWarning($"[GameManager] Player {playerId} already submitted roguelike choice.");
            return;
        }

        var offers = player.UpcomingOffers;
        if (offerIndex < 0 || offerIndex >= offers.PowerOffers.Count)
        {
            Debug.LogWarning($"[GameManager] Player {playerId} invalid Power offer index: {offerIndex}");
            return;
        }

        string cardId = offers.PowerOffers[offerIndex];
        AddCardToDeck(player, cardId);
        Debug.Log($"[GameManager] Player {playerId} chose Power card: {cardId}");

        HandleSubmitRoguelikeChoice(agent);
        StateRelay.Instance.BroadcastToAll();
    }

    [Server]
    public void HandleChooseStrategy(PlayerNetworkAgent agent, int offerIndex)
    {
        int playerId = _registeredPlayers.IndexOf(agent);
        var player = _gameState.GetPlayer(playerId);

        if (CurrentPhase != TurnPhase.RoguelikePhase)
        {
            Debug.LogWarning($"[GameManager] Player {playerId} tried to choose Strategy outside RoguelikePhase.");
            return;
        }

        if (player.UpgradeChoiceSubmitted)
        {
            Debug.LogWarning($"[GameManager] Player {playerId} already submitted roguelike choice.");
            return;
        }

        var offers = player.UpcomingOffers;
        if (offerIndex < 0 || offerIndex >= offers.StrategyOffers.Count)
        {
            Debug.LogWarning($"[GameManager] Player {playerId} invalid Strategy offer index: {offerIndex}");
            return;
        }

        string cardId = offers.StrategyOffers[offerIndex];
        AddCardToDeck(player, cardId);
        Debug.Log($"[GameManager] Player {playerId} chose Strategy card: {cardId}");

        HandleSubmitRoguelikeChoice(agent);
        StateRelay.Instance.BroadcastToAll();
    }

    [Server]
    public void HandleCommitPower(PlayerNetworkAgent agent)
    {
        int playerId = _registeredPlayers.IndexOf(agent);
        var player = _gameState.GetPlayer(playerId);

        if (CurrentPhase != TurnPhase.RoguelikePhase)
        {
            Debug.LogWarning($"[GameManager] Player {playerId} tried to commit Power outside RoguelikePhase.");
            return;
        }
        if (player.UpgradeChoiceSubmitted || player.PowerCategoryCommitted || player.StrategyCategoryCommitted)
        {
            Debug.LogWarning($"[GameManager] Player {playerId} already committed.");
            return;
        }

        player.PowerCategoryCommitted = true;
        Debug.Log($"[GameManager] Player {playerId} committed to Power.");
        StateRelay.Instance.BroadcastToAll();
    }

    [Server]
    public void HandleCommitStrategy(PlayerNetworkAgent agent)
    {
        int playerId = _registeredPlayers.IndexOf(agent);
        var player = _gameState.GetPlayer(playerId);

        if (CurrentPhase != TurnPhase.RoguelikePhase)
        {
            Debug.LogWarning($"[GameManager] Player {playerId} tried to commit Strategy outside RoguelikePhase.");
            return;
        }
        if (player.UpgradeChoiceSubmitted || player.PowerCategoryCommitted || player.StrategyCategoryCommitted)
        {
            Debug.LogWarning($"[GameManager] Player {playerId} already committed.");
            return;
        }

        player.StrategyCategoryCommitted = true;
        Debug.Log($"[GameManager] Player {playerId} committed to Strategy.");
        StateRelay.Instance.BroadcastToAll();
    }

    [Server]
    private void AddCardToDeck(PlayerState player, string cardId)
    {
        var definition = Resources.Load<CardDefinition>($"Cards/{CardIdToAssetName(cardId)}");
        if (definition == null)
        {
            Debug.LogWarning($"[GameManager] Could not load CardDefinition for: {cardId}");
            return;
        }

        int instanceId = player.PlayerId * 100 + player.Deck.Count + player.Discard.Count + player.Hand.Count;
        var instance = new CardInstance(definition, _gameState.GenerateInstanceId());

        // Insert at random position in draw pile rather than shuffling
        int insertAt = _gameState.Rng.Next(player.Deck.Count + 1);
        player.Deck.Insert(insertAt, instance);
        Debug.Log($"[GameManager] Added {cardId} to Player {player.PlayerId}'s deck at position {insertAt}/{player.Deck.Count - 1}.");
    }

    [Server]
    private string CardIdToAssetName(string cardId)
    {
        // Convert snake_case to PascalCase
        var parts = cardId.Split('_');
        return string.Concat(System.Array.ConvertAll(parts,
            p => char.ToUpper(p[0]) + p.Substring(1)));
    }

    [Server]
    private ShopOffer GenerateShopOffer(PlayerState player)
    {
        var offer = new ShopOffer();
        var characterId = player.Character.CharacterId;
        var rng = _gameState.Rng;

        var powerPicks = PickRandom(_cardPool.GetPowerPool(characterId), 2);

        foreach (var card in powerPicks)
        {
            offer.PowerCards.Add(card.CardId);
            offer.PowerDisplayNames.Add(card.DisplayName);
            offer.PowerPrices.Add(ShopPricing.Roll(rng, ShopPricing.PowerCard));
        }

        var strategyPicks = PickRandom(_cardPool.GetStrategyPool(characterId), 2);

        foreach (var card in strategyPicks)
        {
            offer.StrategyCards.Add(card.CardId);
            offer.StrategyDisplayNames.Add(card.DisplayName);
            offer.StrategyPrices.Add(ShopPricing.Roll(rng, ShopPricing.StrategyCard));
        }

        var colorlessPicks = PickRandom(_cardPool.Colorless, 1);

        foreach (var card in colorlessPicks)
        {
            offer.ColorlessCards.Add(card.CardId);
            offer.ColorlessDisplayNames.Add(card.DisplayName);
            offer.ColorlessPrices.Add(ShopPricing.Roll(rng, ShopPricing.ColorlessCard));
        }

        var passivePicks = PickRandomPassives(_cardPool.Passives, 3);

        foreach (var passive in passivePicks)
        {
            offer.Passives.Add(passive.PassiveId);
            offer.PassiveDisplayNames.Add(passive.DisplayName);
            offer.PassivePrices.Add(ShopPricing.Roll(rng, ShopPricing.Passive));
        }

        // Fixed slot costs rolled with variance
        offer.HpRegenSmallCost = ShopPricing.Roll(rng, ShopPricing.HpRegenSmall);
        offer.HpRegenLargeCost = ShopPricing.Roll(rng, ShopPricing.HpRegenLarge);
        offer.SightSmallCost = ShopPricing.Roll(rng, ShopPricing.SightSmall);
        offer.SightLargeCost = ShopPricing.Roll(rng, ShopPricing.SightLarge);
        offer.PerTurnResourceCost = ShopPricing.Roll(rng, ShopPricing.PerTurnResource);

        return offer;
    }

    [Server]
    private List<PassiveDefinition> PickRandomPassives(PassiveDefinition[] pool, int count)
    {
        var result = new List<PassiveDefinition>();
        if (pool == null || pool.Length == 0) return result;

        var indices = new List<int>();
        for (int i = 0; i < pool.Length; i++) indices.Add(i);

        count = Mathf.Min(count, pool.Length);
        for (int i = 0; i < count; i++)
        {
            int pick = _gameState.Rng.Next(indices.Count);
            result.Add(pool[indices[pick]]);
            indices.RemoveAt(pick);
        }
        return result;
    }

    [Server]
    public void HandleShopPurchase(PlayerNetworkAgent agent, ShopPurchaseType purchaseType, int index)
    {
        int playerId = _registeredPlayers.IndexOf(agent);
        var player = _gameState.GetPlayer(playerId);
        var offer = playerId == 0 ? _gameState.Player0ShopOffer : _gameState.Player1ShopOffer;

        if (CurrentPhase != TurnPhase.ShopPhase)
        {
            Debug.LogWarning($"[GameManager] Player {playerId} tried to shop outside ShopPhase.");
            return;
        }

        switch (purchaseType)
        {
            case ShopPurchaseType.PowerCard:
                if (!ValidateShopIndex(index, offer.PowerCards, offer.PowerPrices, player, playerId)) return;
                AddCardToDeck(player, offer.PowerCards[index]);
                player.Silver -= offer.PowerPrices[index];
                offer.PowerCards.RemoveAt(index);
                offer.PowerPrices.RemoveAt(index);
                break;

            case ShopPurchaseType.StrategyCard:
                if (!ValidateShopIndex(index, offer.StrategyCards, offer.StrategyPrices, player, playerId)) return;
                AddCardToDeck(player, offer.StrategyCards[index]);
                player.Silver -= offer.StrategyPrices[index];
                offer.StrategyCards.RemoveAt(index);
                offer.StrategyPrices.RemoveAt(index);
                break;

            case ShopPurchaseType.ColorlessCard:
                if (!ValidateShopIndex(index, offer.ColorlessCards, offer.ColorlessPrices, player, playerId)) return;
                AddCardToDeck(player, offer.ColorlessCards[index]);
                player.Silver -= offer.ColorlessPrices[index];
                offer.ColorlessCards.RemoveAt(index);
                offer.ColorlessPrices.RemoveAt(index);
                break;

            case ShopPurchaseType.Passive:
                if (!ValidateShopIndex(index, offer.Passives, offer.PassivePrices, player, playerId)) return;
                ApplyPassive(player, offer.Passives[index]);
                player.Silver -= offer.PassivePrices[index];
                offer.Passives.RemoveAt(index);
                offer.PassivePrices.RemoveAt(index);
                break;

            case ShopPurchaseType.HpRegenSmall:
                if (player.Silver < offer.HpRegenSmallCost) { LogCannotAfford(playerId, "HpRegenSmall"); return; }
                player.HP = Mathf.Min(player.HP + Mathf.RoundToInt(player.Character.BaseHP * 0.25f), player.Character.BaseHP);
                player.Silver -= offer.HpRegenSmallCost;
                offer.HpRegenSmallCost = -1; // mark as purchased
                break;

            case ShopPurchaseType.HpRegenLarge:
                if (player.Silver < offer.HpRegenLargeCost) { LogCannotAfford(playerId, "HpRegenLarge"); return; }
                player.HP = Mathf.Min(player.HP + Mathf.RoundToInt(player.Character.BaseHP * 0.5f), player.Character.BaseHP);
                player.Silver -= offer.HpRegenLargeCost;
                offer.HpRegenLargeCost = -1;
                break;

            case ShopPurchaseType.SightSmall:
                if (player.Silver < offer.SightSmallCost) { LogCannotAfford(playerId, "SightSmall"); return; }
                player.InsightTree.SightBanked += 2;
                player.Silver -= offer.SightSmallCost;
                offer.SightSmallCost = -1;
                break;

            case ShopPurchaseType.SightLarge:
                if (player.Silver < offer.SightLargeCost) { LogCannotAfford(playerId, "SightLarge"); return; }
                player.InsightTree.SightBanked += 5;
                player.Silver -= offer.SightLargeCost;
                offer.SightLargeCost = -1;
                break;

            case ShopPurchaseType.PerTurnResource:
                if (player.Silver < offer.PerTurnResourceCost) { LogCannotAfford(playerId, "PersistentResource"); return; }
                player.Resources.PerTurnResource += 1;
                player.Silver -= offer.PerTurnResourceCost;
                offer.PerTurnResourceCost = -1;
                break;
        }

        Debug.Log($"[GameManager] Player {playerId} purchased {purchaseType}. Silver remaining: {player.Silver}");
        StateRelay.Instance.BroadcastToAll();
    }

    [Server]
    public void HandleShopDone(PlayerNetworkAgent agent)
    {
        int playerId = _registeredPlayers.IndexOf(agent);

        if (CurrentPhase != TurnPhase.ShopPhase)
        {
            Debug.LogWarning($"[GameManager] Player {playerId} submitted ShopDone outside ShopPhase.");
            return;
        }

        var player = _gameState.GetPlayer(playerId);
        if (player.ShopDoneSubmitted)
        {
            Debug.LogWarning($"[GameManager] Player {playerId} already submitted ShopDone.");
            return;
        }

        player.ShopDoneSubmitted = true;
        _shopVotes++;
        Debug.Log($"[GameManager] Player {playerId} done shopping. Votes: {_shopVotes}/2");

        if (_shopVotes >= 2)
        {
            Debug.Log("[GameManager] Both players done shopping — advancing.");
            AdvancePhase();
        }
    }

    [Server]
    private bool ValidateShopIndex(int index, List<string> items, List<int> prices, PlayerState player, int playerId)
    {
        if (index < 0 || index >= items.Count)
        {
            Debug.LogWarning($"[GameManager] Player {playerId} invalid shop index: {index}");
            return false;
        }
        if (player.Silver < prices[index])
        {
            LogCannotAfford(playerId, items[index]);
            return false;
        }
        return true;
    }

    [Server]
    private void ApplyPassive(PlayerState player, string passiveId)
    {
        var passive = new Passive { PassiveId = passiveId, DisplayName = passiveId, StackCount = 1 };
        player.Passives.Add(passive);
        var effect = PassiveRegistry.Instance.GetEffect(passiveId);
        effect?.OnAcquire(player, _gameState);
        Debug.Log($"[GameManager] Player {player.PlayerId} acquired passive: {passiveId}");
    }

    private void LogCannotAfford(int playerId, string item)
    {
        Debug.LogWarning($"[GameManager] Player {playerId} cannot afford: {item}");
    }

    [Server]
    public void HandleSubmitAuctionBids(PlayerNetworkAgent agent, int bid0, int bid1, int bid2)
    {
        int playerId = _registeredPlayers.IndexOf(agent);
        var player = _gameState.GetPlayer(playerId);
        var auction = _gameState.AuctionOffer;

        if (CurrentPhase != TurnPhase.AuctionPhase)
        {
            Debug.LogWarning($"[GameManager] Player {playerId} submitted bids outside AuctionPhase.");
            return;
        }

        bool alreadySubmitted = playerId == 0 ? auction.Player0Submitted : auction.Player1Submitted;
        if (alreadySubmitted)
        {
            Debug.LogWarning($"[GameManager] Player {playerId} already submitted auction bids.");
            return;
        }

        // Clamp each bid to player's current Silver
        int[] bids = new int[]
        {
            Mathf.Clamp(bid0, 0, player.Silver),
            Mathf.Clamp(bid1, 0, player.Silver),
            Mathf.Clamp(bid2, 0, player.Silver)
        };

        // Log if any bid was clamped
        int[] rawBids = new int[] { bid0, bid1, bid2 };
        for (int i = 0; i < 3; i++)
        {
            if (rawBids[i] != bids[i])
                Debug.LogWarning($"[GameManager] Player {playerId} bid {rawBids[i]} on card {i} but only has {player.Silver} Silver — clamped to {bids[i]}.");
        }

        if (playerId == 0)
        {
            auction.Player0Bids = bids;
            auction.Player0Submitted = true;
        }
        else
        {
            auction.Player1Bids = bids;
            auction.Player1Submitted = true;
        }

        Debug.Log($"[GameManager] Player {playerId} submitted auction bids: {bids[0]}, {bids[1]}, {bids[2]}");

        StateRelay.Instance.BroadcastToAll();

        if (auction.Player0Submitted && auction.Player1Submitted)
        {
            ResolveAuction();
            StateRelay.Instance.BroadcastToAll();
            AdvancePhase();
        }
    }
    private void RevealAllFog()
    {
        RevealPlayerFog(_gameState.Player1);
        RevealPlayerFog(_gameState.Player2);
    }

    private void RevealPlayerFog(PlayerState player)
    {
        player.FogReveals.CharacterIdentity = true;
        player.FogReveals.CharacterHP = true;
        player.FogReveals.CharacterResources = true;
        player.FogReveals.HandSize = true;
        player.FogReveals.HandContents = true;
        player.FogReveals.DrawPileCount = true;
        player.FogReveals.DrawPileContents = true;
        player.FogReveals.DrawPileOrdered = true;
        player.FogReveals.DiscardPileCount = true;
        player.FogReveals.DiscardPileContents = true;
        player.FogReveals.PermanentsOpponentCount = true;
        player.FogReveals.BoardState = true;
        player.FogReveals.PassivesOpponentCount = true;
        player.FogReveals.PassivesOpponent = true;
        player.FogReveals.PastUpgrades = true;
        player.FogReveals.FutureUpgradesOpponent = true;
        player.FogReveals.FutureUpgradesSelf = true;
        player.FogReveals.InsightTreeOpponent = true;
    }
    [Server]
    private void ApplyTurnStartStatusEffects(PlayerState player)
    {
        foreach (var effect in player.StatusEffects)
        {
            switch (effect.EffectId)
            {
                case "bleed":
                    player.TakeDamage(effect.Value);
                    Debug.Log($"[GameManager] Player {player.PlayerId} took {effect.Value} bleed damage.");
                    _gameState.CheckWinCondition();
                    break;
                case "multiculta_daggers":
                    player.Resources.PerTurnResource += effect.Value;
                    Debug.Log($"[GameManager] Player {player.PlayerId} gained {effect.Value} Daggers from Multiculta.");
                    break;
                case "blessed_by_storm":
                    // +2 damage on all attacks this turn — stored as status, read by DealDamageEffect
                    Debug.Log($"[GameManager] Player {player.PlayerId} has Blessed by the Storm (+2 damage).");
                    break;
            }
        }
        player.TickStatusEffects(); // decrement durations, remove expired
    }
    [Server]
    private void AwardSilver(PlayerState player, int amount, string reason)
    {
        player.Silver += amount;
        Debug.Log($"[GameManager] Player {player.PlayerId} earned {amount} Silver ({reason}). Total: {player.Silver}");
    }
    [Server]
    private void ApplyPassiveTurnStart(PlayerState player)
    {
        foreach (var passive in player.Passives)
        {
            var effect = PassiveRegistry.Instance.GetEffect(passive.PassiveId);
            effect?.OnTurnStart(player, _gameState);
        }
    }
}