using UnityEngine;
using UnityEngine.UIElements;
using FogClouds;
using System.Collections.Generic;
using System.Linq;
using Telepathy;

public class GameHUDController : MonoBehaviour
{
    private UIDocument _doc;
    private VisualElement _root;

    // Vitals
    private VisualElement _hpBarFill;
    private Label _hpLabel;
    private Label _shieldLabel;

    // Resources
    private VisualElement _perTurnPips;
    private Label _persistentValue;
    private Label _silverLabel;
    private Label _sightLabel;

    // Deck
    private Label _drawCount;
    private Label _discardCount;

    // Phase / Turn
    private Label _phaseLabel;
    private Label _turnLabel;

    // Board
    private VisualElement _ownBoardZone;
    private VisualElement _opponentBoardZone;

    // Queue
    private VisualElement _ownQueue;
    private VisualElement _mergedQueue;
    private VisualElement _roguelikePanel;
    private VisualElement _categoryButtons;
    private VisualElement _cardOfferButtons;
    private Label _roguelikeWaiting;

    // Opponent display
    private Label _opponentNameLabel;
    private VisualElement _opponentPortrait;
    private Label _opponentPortraitLabel;
    private VisualElement _opponentHpBarFill;
    private Label _opponentHpLabel;
    private Label _opponentShieldLabel;
    private Button _opponentHandCount;
    private Label _opponentDeckCount;
    private Label _opponentDiscardCount;
    private Label _opponentDaggersLabel;
    private Label _opponentPersistentLabel;
    private Label _opponentSightLabel;
    private Label _opponentSilverLabel;

    // INSIGHT overlay
    private VisualElement _insightOverlay;
    private VisualElement _insightContent;
    private Button _tabPastUpgrades;
    private Button _tabFutureOpponent;
    private Button _tabFutureSelf;
    private Button _tabOpponentTree;
    private Button _tabOwnTree;
    private int _activeInsightTab = 0;

    // Pile overlay
    private VisualElement _pileOverlay;
    private Label _pileOverlayTitle;
    private VisualElement _pileContent;

    // Buttons
    private Button _endTurnButton;
    private Button _insightButton;
    private Button _passiveButton;

    //Insight Tree
    [SerializeField] private InsightTreeDefinition _insightTreeDefinition;

    // INSIGHT tree overlay
    private VisualElement _treeOverlay;
    private Label _treeSightLabel;
    private VisualElement _treeContent;
    private bool _treeOpenedByRoguelike = false;

    // Shop panel
    private VisualElement _shopPanel;
    private Label _shopSilverLabel;
    private VisualElement _shopPowerRow;
    private VisualElement _shopStrategyRow;
    private VisualElement _shopColorlessRow;
    private VisualElement _shopPassivesRow;
    private VisualElement _shopServicesRow;
    private Label _shopWaitingLabel;

    // Auction panel
    private VisualElement _auctionPanel;
    private Label _auctionSilverLabel;
    private VisualElement _auctionCardsRow;
    private Label _auctionWaitingLabel;
    private Button _auctionSubmitBtn;
    private int[] _currentBids = new int[] { 0, 0, 0 };

    //Events
    private VisualElement _eventPanel;
    private Label _eventTitleLabel;
    private Label _eventDescLabel;
    private VisualElement _eventContent;
    private Label _eventWaitingLabel;

    private string _fortuneOwnChoice;


    private Dictionary<string, CharacterData> _characterCache = new();
    private Dictionary<string, CardDefinition> _cardDefCache = new();
    private InsightTreeDefinition _cachedInsightTree;

    //passives
    private VisualElement _passivesShelf;

    //targeting
    private bool _targetingActive = false;
    private System.Action<int> _onTargetSelected;
    private System.Action _onTargetCancelled;
    private List<VisualElement> _highlightedChips = new();

    void Start()
    {
        _doc = GetComponent<UIDocument>();
        _root = _doc.rootVisualElement;

        _hpBarFill = _root.Q<VisualElement>("hp-bar-fill");
        _hpLabel = _root.Q<Label>("hp-label");
        _shieldLabel = _root.Q<Label>("shield-label");
        _perTurnPips = _root.Q<VisualElement>("per-turn-pips");
        _persistentValue = _root.Q<Label>("persistent-value");
        _silverLabel = _root.Q<Label>("silver-label");
        _sightLabel = _root.Q<Label>("sight-label");
        _drawCount = _root.Q<Label>("draw-count");
        _discardCount = _root.Q<Label>("discard-count");
        _phaseLabel = _root.Q<Label>("phase-label");
        _turnLabel = _root.Q<Label>("turn-label");
        _ownBoardZone = _root.Q<VisualElement>("own-board");
        _opponentBoardZone = _root.Q<VisualElement>("opponent-board");
        _ownQueue = _root.Q<VisualElement>("own-queue");
        _mergedQueue = _root.Q<VisualElement>("merged-queue");
        _roguelikePanel = _root.Q<VisualElement>("roguelike-panel");
        _categoryButtons = _root.Q<VisualElement>("category-buttons");
        _cardOfferButtons = _root.Q<VisualElement>("card-offer-buttons");
        _roguelikeWaiting = _root.Q<Label>("roguelike-waiting");
        _endTurnButton = _root.Q<Button>("end-turn-button");
        _insightButton = _root.Q<Button>("insight-button");
        _passiveButton = _root.Q<Button>("passive-button");
        _opponentNameLabel = _root.Q<Label>("opponent-name-label");
        _opponentPortrait = _root.Q<VisualElement>("opponent-portrait");
        _opponentPortraitLabel = _root.Q<Label>("opponent-portrait-label");
        _opponentHpBarFill = _root.Q<VisualElement>("opponent-hp-bar-fill");
        _opponentHpLabel = _root.Q<Label>("opponent-hp-label");
        _opponentShieldLabel = _root.Q<Label>("opponent-shield-label");
        _opponentHandCount = _root.Q<Button>("opponent-hand-btn");
        _opponentDeckCount = _root.Q<Label>("opponent-deck-count");
        _opponentDiscardCount = _root.Q<Label>("opponent-discard-count");
        _opponentDaggersLabel = _root.Q<Label>("opponent-daggers-label");
        _opponentPersistentLabel = _root.Q<Label>("opponent-persistent-label");
        _opponentSightLabel = _root.Q<Label>("opponent-sight-label");
        _opponentSilverLabel = _root.Q<Label>("opponent-silver-label");
        _insightOverlay = _root.Q<VisualElement>("insight-overlay");
        _insightContent = _root.Q<VisualElement>("insight-content");
        _tabPastUpgrades = _root.Q<Button>("tab-past-upgrades");
        _tabFutureOpponent = _root.Q<Button>("tab-future-opponent");
        _tabFutureSelf = _root.Q<Button>("tab-future-self");
        _tabOpponentTree = _root.Q<Button>("tab-opponent-tree");
        _pileOverlay = _root.Q<VisualElement>("pile-overlay");
        _pileOverlayTitle = _root.Q<Label>("pile-overlay-title");
        _pileContent = _root.Q<VisualElement>("pile-content");
        _treeOverlay = _root.Q<VisualElement>("tree-overlay");
        _treeSightLabel = _root.Q<Label>("tree-sight-label");
        _treeContent = _root.Q<VisualElement>("tree-content");
        _tabOwnTree = _root.Q<Button>("tab-own-tree");
        _shopPanel = _root.Q<VisualElement>("shop-panel");
        _shopSilverLabel = _root.Q<Label>("shop-silver-label");
        _shopPowerRow = _root.Q<VisualElement>("shop-power-row");
        _shopStrategyRow = _root.Q<VisualElement>("shop-strategy-row");
        _shopColorlessRow = _root.Q<VisualElement>("shop-colorless-row");
        _shopPassivesRow = _root.Q<VisualElement>("shop-passives-row");
        _shopServicesRow = _root.Q<VisualElement>("shop-services-row");
        _shopWaitingLabel = _root.Q<Label>("shop-waiting-label");
        _auctionPanel = _root.Q<VisualElement>("auction-panel");
        _auctionSilverLabel = _root.Q<Label>("auction-silver-label");
        _auctionCardsRow = _root.Q<VisualElement>("auction-cards-row");
        _auctionWaitingLabel = _root.Q<Label>("auction-waiting-label");
        _auctionSubmitBtn = _root.Q<Button>("auction-submit-btn");
        _eventPanel = _root.Q<VisualElement>("event-panel");
        _eventTitleLabel = _root.Q<Label>("event-title-label");
        _eventDescLabel = _root.Q<Label>("event-desc-label");
        _eventContent = _root.Q<VisualElement>("event-content");
        _eventWaitingLabel = _root.Q<Label>("event-waiting-label");
        _passivesShelf = _root.Q<VisualElement>("passives-shelf");

        // Tab buttons
        _tabPastUpgrades.clicked += () => OpenInsightTab(0);
        _tabFutureOpponent.clicked += () => OpenInsightTab(1);
        _tabFutureSelf.clicked += () => OpenInsightTab(2);
        _tabOpponentTree.clicked += () => OpenInsightTab(3);
        _tabOwnTree.clicked += () => OpenInsightTab(4);

        _endTurnButton.clicked += OnEndTurnClicked;
        _insightButton.clicked += OnInsightClicked;
        _passiveButton.clicked += OnPassiveClicked;
        _auctionSubmitBtn.clicked += OnAuctionSubmit;
        _opponentHandCount.clicked += OnOpponentHandClicked;

        //Buttons
        _root.Q<Button>("insight-overlay-close").clicked += () =>
            _insightOverlay.style.display = DisplayStyle.None;
        _root.Q<Button>("pile-overlay-close").clicked += () =>
            _pileOverlay.style.display = DisplayStyle.None;
        _root.Q<Button>("draw-pile-btn").clicked += OnOwnDrawPileClicked;
        _root.Q<Button>("discard-pile-btn").clicked += OnOwnDiscardPileClicked;
        _root.Q<Button>("opponent-deck-btn").clicked += OnOpponentDrawPileClicked;
        _root.Q<Button>("opponent-discard-btn").clicked += OnOpponentDiscardPileClicked;
        _root.Q<Button>("power-btn").clicked += () => PlayerNetworkAgent.LocalAgent?.CmdCommitPower();
        _root.Q<Button>("strategy-btn").clicked += () => PlayerNetworkAgent.LocalAgent?.CmdCommitStrategy();
        _root.Q<Button>("insight-btn-roguelike").clicked += OnRoguelikeInsightClicked;
        _root.Q<Button>("shop-done-btn").clicked += () =>
            {
                PlayerNetworkAgent.LocalAgent?.CmdShopDone();
            };
        _root.Q<Button>("tree-overlay-close").clicked += () =>
        {
            _treeOverlay.style.display = DisplayStyle.None;
            if (_treeOpenedByRoguelike)
            {
                _treeOpenedByRoguelike = false;
                _categoryButtons.style.display = DisplayStyle.None;
                _cardOfferButtons.style.display = DisplayStyle.None;
                _roguelikeWaiting.style.display = DisplayStyle.Flex;
                PlayerNetworkAgent.LocalAgent?.CmdSubmitRoguelikeChoice();
            }
        };

        var handController = GetComponent<HandController>();
        handController?.Initialize(_root);

        var opponentHandController = GetComponent<OpponentHandController>();
        opponentHandController?.Initialize(_root);

        var tooltipController = GetComponent<TooltipController>();
        tooltipController?.Initialize(_root);

        if (ClientStateManager.Instance != null)
        {
            ClientStateManager.Instance.OnStateUpdated += Refresh;
            ClientStateManager.Instance.OnStateUpdated += OnStateUpdatedForTree;
        }
        else
            Debug.LogWarning("[GameHUDController] ClientStateManager not found in Start.");
        Debug.Log($"[HUD] drawCount null? {_drawCount == null}, discardCount null? {_discardCount == null}");
    }

    void OnDisable()
    {
        if (ClientStateManager.Instance != null)
        {
            ClientStateManager.Instance.OnStateUpdated -= Refresh;
            ClientStateManager.Instance.OnStateUpdated -= OnStateUpdatedForTree;
        }

    }

    private bool _firstRefresh = true;

    private void Refresh(ClientGameStateView view)
    {
        if (_firstRefresh)
        {
            _firstRefresh = false;
            StartCoroutine(DeferredRefresh(view));
            return;
        }
        DoRefresh(view);
    }

    private System.Collections.IEnumerator DeferredRefresh(ClientGameStateView view)
    {
        yield return null;
#if UNITY_EDITOR
        yield return null;
#endif
        while (_root?.panel == null) yield return null;
        DoRefresh(view);
    }

    private void DoRefresh(ClientGameStateView view)
    {

        var own = view.OwnState;
        if (own == null) return;

        RefreshOpponentDisplay(view);

        Debug.Log($"[GameHUDController] Refresh — CharacterId: {own.CharacterId}, HP: {own.HP}, BaseHP: {LoadBaseHP(own.CharacterId)}");

        // Phase and turn
        _phaseLabel.text = view.CurrentPhase.ToString();
        _turnLabel.text = $"Turn {view.TurnNumber}";

        // HP bar
        float hpPercent = own.HP / (float)LoadBaseHP(own.CharacterId);
        _hpBarFill.style.width = Length.Percent(hpPercent * 100f);
        _hpLabel.text = $"{own.HP}/{LoadBaseHP(own.CharacterId)}";

        // Shield
        _shieldLabel.text = own.Shield > 0 ? $"{own.Shield}" : "";

        // Resources
        RefreshPips(own.Resources);
        _persistentValue.text = own.Resources?.PersistentResource.ToString() ?? "0";
        _silverLabel.text = $"Silver: {own.Silver}";
        _sightLabel.text = $"Sight: {own.InsightTree?.SightBanked ?? 0}";

        // Deck
        _drawCount.text = own.DeckCount.ToString();
        _discardCount.text = own.DiscardCount.ToString();
        Debug.Log($"[HUD] DeckCount: {own.DeckCount}, DiscardCount: {own.DiscardCount}");

        //Board
        RefreshOwnBoard(own.Board);
        RefreshOpponentBoard(view.OpponentState?.Board);

        // Queue visibility
        bool isMerged = view.CurrentPhase == TurnPhase.QueueResolution || view.CurrentPhase == TurnPhase.QueueMerge;
        _mergedQueue.style.display = isMerged ? DisplayStyle.Flex : DisplayStyle.None;
        _ownQueue.style.display = isMerged ? DisplayStyle.None : DisplayStyle.Flex;

        RefreshOwnQueue(view.OwnQueue);
        RefreshMergedQueue(view.MergedQueue, isMerged);

        // Button visibility
        bool isMainPhase = view.CurrentPhase == TurnPhase.MainPhase;
        _endTurnButton.style.display = isMainPhase ? DisplayStyle.Flex : DisplayStyle.None;
        _endTurnButton.SetEnabled(!view.ReadyToEndTurn);
        _endTurnButton.text = view.ReadyToEndTurn ? "Waiting..." : "End Turn";



        RefreshRoguelikePanel(view);
        RefreshShopPanel(view);
        RefreshAuctionPanel(view);
        RefreshEventPanel(view);

        // Show passive button only if player has interactive passives
        _passiveButton.style.display = DisplayStyle.None;
        RefreshPassivesShelf(own, view);

        if (view.GameOver)
        {
            ShowGameOverScreen(view);
            return;
        }
    }

    private void RefreshPips(ResourceState resources)
    {
        if (_perTurnPips == null || _perTurnPips.panel == null) return;
        if (resources == null) return;

        int desired = resources.PerTurnResourceMax;

        _perTurnPips.schedule.Execute(() =>
        {
            // Add missing pips
            while (_perTurnPips.childCount < desired)
            {
                var pip = new VisualElement();
                _perTurnPips.Add(pip);
            }
            // Remove excess pips
            while (_perTurnPips.childCount > desired)
                _perTurnPips.RemoveAt(_perTurnPips.childCount - 1);

            // Update classes in place — no structural mutation
            for (int i = 0; i < desired; i++)
            {
                var pip = _perTurnPips[i];
                pip.EnableInClassList("pip-filled", i < resources.PerTurnResource);
                pip.EnableInClassList("pip-empty", i >= resources.PerTurnResource);
            }

        });
    }

    private CharacterData GetCharacterData(string characterId)
    {
        if (string.IsNullOrEmpty(characterId)) return null;
        if (!_characterCache.TryGetValue(characterId, out var data))
        {
            data = Resources.Load<CharacterData>($"Characters/{characterId}");
            if (data != null) _characterCache[characterId] = data;
        }
        return data;
    }

    private CardDefinition GetCardDef(string cardId)
    {
        if (string.IsNullOrEmpty(cardId)) return null;
        if (!_cardDefCache.TryGetValue(cardId, out var def))
        {
            def = Resources.Load<CardDefinition>($"Cards/{cardId}");
            if (def != null) _cardDefCache[cardId] = def;
        }
        return def;
    }

    private InsightTreeDefinition GetInsightTreeDef()
    {
        if (_cachedInsightTree == null)
            _cachedInsightTree = Resources.Load<InsightTreeDefinition>("TreeNodes/InsightTree");
        return _cachedInsightTree;
    }

    private int LoadBaseHP(string characterId)
    {
        return GetCharacterData(characterId)?.BaseHP ?? 40;
    }

    // Button handlers — route to PlayerNetworkAgent
    private void OnEndTurnClicked()
    {
        PlayerNetworkAgent.LocalAgent?.CmdSubmitEndTurn();
    }

    private void OnInsightClicked()
    {
        _insightOverlay.style.display = DisplayStyle.Flex;
        OpenInsightTab(_activeInsightTab);
    }
    private void OnPassiveClicked()
    {
        // Phase 6: trigger interactive passive
        Debug.Log("[GameHUDController] Passive button clicked.");
    }
    private void RefreshOwnQueue(List<QueueEntryView> entries)
    {
        if (_ownQueue == null || _ownQueue.panel == null) return;

        _ownQueue.schedule.Execute(() =>
        {
            _ownQueue.Clear();
            if (entries == null || entries.Count == 0) return;

            foreach (var entry in entries)
            {
                var row = new VisualElement();
                row.AddToClassList("queue-entry");

                var nameLabel = new Label(entry.WasUpcast ? $"{entry.Card.DisplayName} ★" : entry.Card.DisplayName);
                nameLabel.AddToClassList("queue-entry-name");

                var speedLabel = new Label($"SPD {entry.CurrentSpeed}");
                speedLabel.AddToClassList("queue-entry-speed");

                row.Add(nameLabel);
                row.Add(speedLabel);
                _ownQueue.Add(row);
            }
        });
    }

    private void RefreshMergedQueue(List<QueueEntryView> entries, bool visible)
    {
        if (_mergedQueue == null || _mergedQueue.panel == null) return;

        _mergedQueue.schedule.Execute(() =>
        {
            _mergedQueue.Clear();
            if (!visible || entries == null || entries.Count == 0) return;

            var own = ClientStateManager.Instance?.CurrentState?.OwnState;
            int myPlayerId = own?.PlayerId ?? -1;

            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                var row = new VisualElement();
                row.AddToClassList("queue-entry");
                if (i == 0) row.AddToClassList("queue-entry-resolving");

                bool isOwn = entry.OwnerId == myPlayerId;
                row.AddToClassList(isOwn ? "queue-entry-own" : "queue-entry-opponent");

                var nameLabel = new Label(entry.WasUpcast ? $"{entry.Card.DisplayName} ★" : entry.Card.DisplayName);
                nameLabel.AddToClassList("queue-entry-name");

                var speedLabel = new Label($"SPD {entry.CurrentSpeed}");
                speedLabel.AddToClassList("queue-entry-speed");

                row.Add(nameLabel);
                row.Add(speedLabel);
                _mergedQueue.Add(row);
            }
        });
    }

    private void RefreshRoguelikePanel(ClientGameStateView view)
    {
        bool isRoguelike = view.CurrentPhase == TurnPhase.RoguelikePhase;
        _roguelikePanel.style.display = isRoguelike ? DisplayStyle.Flex : DisplayStyle.None;

        if (!isRoguelike)
        {
            _treeOpenedByRoguelike = false;
            return;
        }

        var own = view.OwnState;

        if (own.UpgradeChoiceSubmitted)
        {
            _categoryButtons.style.display = DisplayStyle.None;
            _cardOfferButtons.style.display = DisplayStyle.None;
            _roguelikeWaiting.style.display = DisplayStyle.Flex;
            return;
        }

        // Auto-open tree when INSIGHT committed but not yet submitted
        if (own.InsightCategoryCommitted && !_treeOpenedByRoguelike)
        {
            _treeOpenedByRoguelike = true;
            OpenTreeOverlay(fromRoguelike: true);
        }

        // Hide category buttons once any category committed
        if (own.InsightCategoryCommitted || own.PowerCategoryCommitted || own.StrategyCategoryCommitted)
            _categoryButtons.style.display = DisplayStyle.None;
        else
            _categoryButtons.style.display = DisplayStyle.Flex;
        _roguelikeWaiting.style.display = DisplayStyle.None;

        if (!own.PowerCategoryCommitted && !own.StrategyCategoryCommitted)
        {
            _categoryButtons.style.display = DisplayStyle.Flex;
            _cardOfferButtons.style.display = DisplayStyle.None;
            return;
        }

        // Category committed — show card offers
        _categoryButtons.style.display = DisplayStyle.None;

        var offers = own.FutureOffers;
        var offerList = own.PowerCategoryCommitted ? offers?.PowerOffers : offers?.StrategyOffers;
        var priceList = own.PowerCategoryCommitted ? offers?.PowerPrices : offers?.StrategyPrices;

        _cardOfferButtons.schedule.Execute(() =>
            {
                _cardOfferButtons.Clear();
                _cardOfferButtons.style.display = DisplayStyle.Flex;
                if (offerList == null) return;

                var nameList = own.PowerCategoryCommitted
                    ? offers?.PowerDisplayNames
                    : offers?.StrategyDisplayNames;

                for (int i = 0; i < offerList.Count; i++)
                {
                    int index = i;
                    string cardId = offerList[i];
                    string displayName = nameList != null && i < nameList.Count ? nameList[i] : offerList[i];

                    var btn = new Button();
                    btn.AddToClassList("card-offer-btn");
                    btn.text = displayName;

                    var offerDef = Resources.Load<CardDefinition>($"Cards/{CardIdToAssetName(cardId)}");
                    string tooltipBody = GetCardTooltipBody(cardId,
                        offerDef?.Type ?? CardType.Queueable,
                        offerDef?.BaseSpeed ?? 0);

                    btn.RegisterCallback<PointerEnterEvent>(evt =>
                        TooltipController.Instance?.Show(displayName, tooltipBody, evt.position));
                    btn.RegisterCallback<PointerLeaveEvent>(_ => TooltipController.Instance?.Hide());

                    btn.clicked += () =>
                    {
                        if (own.PowerCategoryCommitted)
                            PlayerNetworkAgent.LocalAgent?.CmdChoosePower(index);
                        else
                            PlayerNetworkAgent.LocalAgent?.CmdChooseStrategy(index);
                    };
                    _cardOfferButtons.Add(btn);
                }
            });
    }
    private void RefreshOpponentDisplay(ClientGameStateView view)
    {
        var opp = view.OpponentState;
        if (opp == null) return;

        // Identity
        bool identityKnown = !string.IsNullOrEmpty(opp.CharacterId);
        string opponentDisplayName = identityKnown
            ? (GetCharacterData(opp.CharacterId)?.DisplayName ?? opp.CharacterId)
            : "???";
        _opponentNameLabel.text = opponentDisplayName;
        _opponentPortraitLabel.text = identityKnown ? "" : "?";

        // HP
        if (opp.HP >= 0 && identityKnown)
        {
            int baseHP = LoadBaseHP(opp.CharacterId);
            float hpPercent = opp.HP / (float)baseHP;
            _opponentHpBarFill.style.width = Length.Percent(hpPercent * 100f);
            _opponentHpLabel.text = $"{opp.HP}/{baseHP}";
        }
        else
        {
            _opponentHpBarFill.style.width = Length.Percent(100f);
            _opponentHpLabel.text = "?";
        }

        // Shield
        _opponentShieldLabel.text = opp.Shield > 0 ? $"{opp.Shield}" : "";

        // Hand / Deck / Discard counts
        _opponentHandCount.text = opp.HandSize >= 0 ? opp.HandSize.ToString() : "?";
        _opponentDeckCount.text = opp.DeckCount >= 0 ? opp.DeckCount.ToString() : "?";
        _opponentDiscardCount.text = opp.DiscardCount >= 0 ? opp.DiscardCount.ToString() : "?";

        // Resources
        if (opp.Resources != null)
        {
            _opponentDaggersLabel.text = $"Daggers: {opp.Resources.PerTurnResource}/{opp.Resources.PerTurnResourceMax}";
            _opponentPersistentLabel.text = $"Persistent: {opp.Resources.PersistentResource}";
        }
        else
        {
            _opponentDaggersLabel.text = "Daggers: ?";
            _opponentPersistentLabel.text = "Persistent: ?";
        }

        _opponentSightLabel.text = opp.InsightTree != null
            ? $"Sight: {opp.InsightTree.SightBanked}"
            : "Sight: ?";

        _opponentSilverLabel.text = opp.Silver >= 0 ? $"Silver: {opp.Silver}" : "Silver: ?";
    }
    private void OpenInsightTab(int tab)
    {
        _activeInsightTab = tab;

        var tabs = new[] { _tabPastUpgrades, _tabFutureOpponent, _tabFutureSelf, _tabOpponentTree, _tabOwnTree };
        for (int i = 0; i < tabs.Length; i++)
            tabs[i].EnableInClassList("insight-tab-active", i == tab);

        // Tree tabs need column layout, offer/upgrade tabs need row wrap
        bool isTreeTab = tab == 3 || tab == 4;
        _insightContent.style.flexDirection = isTreeTab ? FlexDirection.Column : FlexDirection.Row;
        _insightContent.style.flexWrap = isTreeTab ? Wrap.NoWrap : Wrap.Wrap;

        var view = ClientStateManager.Instance?.CurrentState;
        if (view == null) return;

        _insightContent.schedule.Execute(() =>
        {
            _insightContent.Clear();
            switch (tab)
            {
                case 0: PopulatePastUpgrades(view); break;
                case 1: PopulateFutureOpponent(view); break;
                case 2: PopulateFutureSelf(view); break;
                case 3: PopulateOpponentTree(view); break;
                case 4: PopulateOwnTree(view); break;
            }
        });
    }

    private void PopulatePastUpgrades(ClientGameStateView view)
    {
        var upgrades = view.OpponentState?.PastUpgrades;
        if (upgrades == null)
        {
            AddLockedLabel("Requires Alteration.");
            return;
        }
        if (upgrades.Count == 0)
        {
            AddLockedLabel("They have no upgrades. Interesting.");
            return;
        }
        foreach (var cardId in upgrades)
        {
            var def = GetCardDef(cardId);
            string name = def != null ? def.DisplayName : cardId;
            var card = MakeMiniCard(name);
            _insightContent.Add(card);
        }
    }

    private void PopulateFutureOpponent(ClientGameStateView view)
    {
        var offers = view.OpponentState?.FutureOffers;
        if (offers == null)
        {
            AddLockedLabel("Requires Augury.");
            return;
        }
        AddOfferSection("Power", offers.PowerOffers, offers.PowerDisplayNames);
        AddOfferSection("Strategy", offers.StrategyOffers, offers.StrategyDisplayNames);
    }

    private void PopulateFutureSelf(ClientGameStateView view)
    {
        var offers = view.OwnState?.FutureOffers;
        if (offers == null || (offers.PowerOffers.Count == 0 && offers.StrategyOffers.Count == 0))
        {
            AddLockedLabel("Requires Foresight.");
            return;
        }
        AddOfferSection("Power", offers.PowerOffers, offers.PowerDisplayNames);
        AddOfferSection("Strategy", offers.StrategyOffers, offers.StrategyDisplayNames);
    }

    private void PopulateOpponentTree(ClientGameStateView view)
    {
        var tree = view.OpponentState?.InsightTree;
        var insightDef = GetInsightTreeDef();

        if (tree == null)
        {
            AddLockedLabel("Requires Enlightenment.");
            return;
        }
        if (insightDef == null)
        {
            AddLockedLabel("InsightTreeDefinition asset not found.");
            return;
        }

        foreach (var node in insightDef.AllNodes)
        {
            bool unlocked = tree.IsUnlocked(node.NodeId);
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginBottom = 6;

            var dot = new VisualElement();
            dot.style.width = 10;
            dot.style.height = 10;
            dot.style.borderTopLeftRadius = 5;
            dot.style.borderTopRightRadius = 5;
            dot.style.borderBottomLeftRadius = 5;
            dot.style.borderBottomRightRadius = 5;
            dot.style.backgroundColor = unlocked
                ? new StyleColor(new Color(0.4f, 0.8f, 0.4f))
                : new StyleColor(new Color(0.3f, 0.3f, 0.4f));
            dot.style.marginRight = 8;
            dot.style.flexShrink = 0;

            var label = new Label($"{node.DisplayName} ({node.Cost} Sight)");
            label.style.fontSize = 12;
            label.style.color = unlocked
                ? new StyleColor(new Color(0.85f, 0.85f, 0.9f))
                : new StyleColor(new Color(0.45f, 0.45f, 0.55f));

            row.Add(dot);
            row.Add(label);
            _insightContent.Add(row);
        }
    }

    private void AddOfferSection(string title, List<string> cardIds, List<string> displayNames)
    {
        if (cardIds == null || cardIds.Count == 0) return;

        var header = new Label(title);
        header.style.fontSize = 11;
        header.style.color = new StyleColor(new Color(0.6f, 0.6f, 0.7f));
        header.style.marginBottom = 4;
        header.style.marginTop = 8;
        header.style.width = Length.Percent(100);
        _insightContent.Add(header);

        for (int i = 0; i < cardIds.Count; i++)
        {
            string name = displayNames != null && i < displayNames.Count
                ? displayNames[i]
                : cardIds[i];
            _insightContent.Add(MakeMiniCard(name));
        }
    }

    private void AddLockedLabel(string message)
    {
        var label = new Label(message);
        label.AddToClassList("overlay-locked-label");
        _insightContent.Add(label);
    }

    private VisualElement MakeMiniCard(string displayName, CardInstanceView data = null)
    {
        var wrap = new VisualElement();
        wrap.AddToClassList("mini-card-wrap");

        var template = Resources.Load<VisualTreeAsset>("UI/CardView");
        template.CloneTree(wrap);

        var cardRoot = wrap.Q<VisualElement>("card-root");
        cardRoot.style.width = 64;
        cardRoot.style.height = 90;
        cardRoot.AddToClassList("opponent-card");

        wrap.Q<Label>("card-name").text = displayName;
        wrap.Q<VisualElement>("dagger-pips").Clear();
        wrap.Q<Label>("blood-cost").style.display = DisplayStyle.None;
        wrap.Q<Label>("speed-label").style.display = DisplayStyle.None;
        var flavour = wrap.Q<Label>("flavour-text");
        if (flavour != null) flavour.style.display = DisplayStyle.None;

        if (data != null)
        {
            string body = GetCardTooltipBody(data.CardId, data.Type, data.ModifiedSpeed);
            wrap.RegisterCallback<PointerEnterEvent>(evt =>
                TooltipController.Instance?.Show(displayName, body, evt.position));
            wrap.RegisterCallback<PointerLeaveEvent>(_ => TooltipController.Instance?.Hide());
        }

        return wrap;
    }

    // Pile click handlers
    private void OnOwnDrawPileClicked()
    {
        var view = ClientStateManager.Instance?.CurrentState;
        if (view?.OwnState?.Deck == null) return;
        OpenPileViewer("Your Draw Pile", view.OwnState.Deck, ordered: false);
    }

    private void OnOwnDiscardPileClicked()
    {
        var view = ClientStateManager.Instance?.CurrentState;
        if (view?.OwnState?.Discard == null) return;
        OpenPileViewer("Your Discard Pile", view.OwnState.Discard, ordered: true);
    }

    private void OnOpponentDrawPileClicked()
    {
        var view = ClientStateManager.Instance?.CurrentState;
        var deck = view?.OpponentState?.Deck;
        if (deck == null)
        {
            OpenPileViewerLocked("Opponent Draw Pile", "Draw pile contents not yet revealed.");
            return;
        }
        OpenPileViewer("Opponent Draw Pile", deck, ordered: false);
    }

    private void OnOpponentDiscardPileClicked()
    {
        var view = ClientStateManager.Instance?.CurrentState;
        var discard = view?.OpponentState?.Discard;
        if (discard == null)
        {
            OpenPileViewerLocked("Opponent Discard Pile", "Discard pile contents not yet revealed.");
            return;
        }
        OpenPileViewer("Opponent Discard Pile", discard, ordered: true);
    }

    private void OnOpponentHandClicked()
    {
        var view = ClientStateManager.Instance?.CurrentState;
        var hand = view?.OpponentState?.Hand;
        if (hand == null)
        {
            OpenPileViewerLocked("Opponent Hand", "Hand contents not yet revealed.");
            return;
        }
        OpenPileViewer("Opponent Hand", hand, ordered: true);
    }

    private void OpenPileViewer(string title, List<CardInstanceView> cards, bool ordered)
    {
        _pileOverlayTitle.text = title;
        _pileOverlay.style.display = DisplayStyle.Flex;

        _pileContent.schedule.Execute(() =>
        {
            _pileContent.Clear();

            var list = ordered ? cards : new List<CardInstanceView>(cards);
            if (!ordered)
            {
                // Shuffle display order for draw pile — don't reveal ordering
                var rng = new System.Random();
                for (int i = list.Count - 1; i > 0; i--)
                {
                    int j = rng.Next(i + 1);
                    var tmp = list[i]; list[i] = list[j]; list[j] = tmp;
                }
            }

            foreach (var card in list)
                _pileContent.Add(MakeMiniCard(card.DisplayName ?? card.CardId, card));
        });
    }

    private void OpenPileViewerLocked(string title, string message)
    {
        _pileOverlayTitle.text = title;
        _pileOverlay.style.display = DisplayStyle.Flex;
        _pileContent.schedule.Execute(() =>
        {
            _pileContent.Clear();
            var label = new Label(message);
            label.AddToClassList("overlay-locked-label");
            _pileContent.Add(label);
        });
    }
    private void OpenTreeOverlay(bool fromRoguelike)
    {
        _treeOpenedByRoguelike = fromRoguelike;
        _treeOverlay.style.display = DisplayStyle.Flex;
        RefreshTreeOverlay();
    }

    private void RefreshTreeOverlay()
    {
        var view = ClientStateManager.Instance?.CurrentState;
        if (view == null) return;

        var own = view.OwnState;
        int sight = own.InsightTree?.SightBanked ?? 0;
        _treeSightLabel.text = $"Sight Available: {sight}";

        _treeContent.schedule.Execute(() =>
        {
            _treeContent.Clear();

            if (_insightTreeDefinition == null)
            {
                var err = new Label("InsightTreeDefinition not assigned.");
                err.AddToClassList("overlay-locked-label");
                _treeContent.Add(err);
                return;
            }

            var allNodes = _insightTreeDefinition.AllNodes.ToList();
            var sorted = new List<InsightTreeNode>();
            var visited = new HashSet<string>();

            void Visit(InsightTreeNode node)
            {
                if (visited.Contains(node.NodeId)) return;
                visited.Add(node.NodeId);
                if (node.Prerequisites != null)
                    foreach (var prereq in node.Prerequisites)
                    {
                        var prereqNode = allNodes.FirstOrDefault(n => n.NodeId == prereq.NodeId);
                        if (prereqNode != null) Visit(prereqNode);
                    }
                sorted.Add(node);
            }

            foreach (var node in allNodes)
                Visit(node);

            foreach (var node in sorted)
            {
                bool unlocked = own.InsightTree?.IsUnlocked(node.NodeId) ?? false;

                // Check prerequisites
                bool prereqsMet = true;
                if (node.Prerequisites != null)
                {
                    foreach (var prereq in node.Prerequisites)
                    {
                        if (!(own.InsightTree?.IsUnlocked(prereq.NodeId) ?? false))
                        {
                            prereqsMet = false;
                            break;
                        }
                    }
                }

                bool canAfford = sight >= node.Cost;
                bool purchasable = !unlocked && prereqsMet && canAfford;

                var row = new VisualElement();
                row.AddToClassList("tree-node-row");
                row.style.backgroundColor = unlocked
                    ? new StyleColor(new Color(0.15f, 0.25f, 0.15f, 0.8f))
                    : prereqsMet
                        ? new StyleColor(new Color(0.15f, 0.15f, 0.25f, 0.8f))
                        : new StyleColor(new Color(0.1f, 0.1f, 0.15f, 0.5f));

                var dot = new VisualElement();
                dot.AddToClassList("tree-node-dot");
                dot.style.backgroundColor = unlocked
                    ? new StyleColor(new Color(0.3f, 0.8f, 0.3f))
                    : prereqsMet
                        ? new StyleColor(new Color(0.5f, 0.5f, 0.8f))
                        : new StyleColor(new Color(0.3f, 0.3f, 0.35f));
                row.Add(dot);

                var info = new VisualElement();
                info.AddToClassList("tree-node-info");

                var nameLabel = new Label(node.DisplayName);
                nameLabel.AddToClassList("tree-node-name");
                nameLabel.style.color = unlocked
                    ? new StyleColor(new Color(0.7f, 0.95f, 0.7f))
                    : prereqsMet
                        ? new StyleColor(new Color(0.85f, 0.85f, 0.95f))
                        : new StyleColor(new Color(0.4f, 0.4f, 0.5f));
                info.Add(nameLabel);

                var descLabel = new Label(node.Description ?? node.FlagName);
                descLabel.AddToClassList("tree-node-desc");
                info.Add(descLabel);

                if (!prereqsMet && node.Prerequisites != null && node.Prerequisites.Length > 0)
                {
                    var prereqLabel = new Label($"Requires: {string.Join(", ", System.Array.ConvertAll(node.Prerequisites, p => p.DisplayName))}");
                    prereqLabel.AddToClassList("tree-node-prereq");
                    info.Add(prereqLabel);
                }
                row.Add(info);

                if (unlocked)
                {
                    var unlockedLabel = new Label("✓ Unlocked");
                    unlockedLabel.AddToClassList("tree-node-unlocked-label");
                    row.Add(unlockedLabel);
                }
                else if (purchasable)
                {
                    var btn = new Button();
                    btn.text = $"Unlock ({node.Cost} Sight)";
                    btn.AddToClassList("tree-node-unlock-btn");
                    string nodeId = node.NodeId;
                    btn.clicked += () => PlayerNetworkAgent.LocalAgent?.CmdUnlockNode(nodeId);
                    row.Add(btn);
                }
                else if (prereqsMet)
                {
                    var costLabel = new Label($"{node.Cost} Sight");
                    costLabel.AddToClassList("tree-node-cost-label");
                    row.Add(costLabel);
                }

                _treeContent.Add(row);
            }
        });
    }
    private void OnStateUpdatedForTree(ClientGameStateView view)
    {
        if (_treeOverlay?.style.display == DisplayStyle.Flex)
            RefreshTreeOverlay();
        if (_insightOverlay?.style.display == DisplayStyle.Flex)
            OpenInsightTab(_activeInsightTab);
    }
    private void OnRoguelikeInsightClicked()
    {
        var view = ClientStateManager.Instance?.CurrentState;
        if (view?.OwnState?.UpgradeChoiceSubmitted == true) return;

        // Fire the Cmd first to lock the choice server-side
        PlayerNetworkAgent.LocalAgent?.CmdChooseInsight();
        // Tree will auto-open via RefreshRoguelikePanel when UpgradeChoiceSubmitted becomes true
    }
    private void PopulateOwnTree(ClientGameStateView view)
    {
        var own = view.OwnState;
        var tree = own.InsightTree;
        int sight = tree?.SightBanked ?? 0;

        if (_insightTreeDefinition == null)
        {
            AddLockedLabel("InsightTreeDefinition not assigned.");
            return;
        }

        // Sight balance header
        var sightHeader = new Label($"Sight Available: {sight}");
        sightHeader.style.fontSize = 12;
        sightHeader.style.color = new StyleColor(new Color(0.6f, 0.85f, 0.55f));
        sightHeader.style.marginBottom = 10;
        _insightContent.Add(sightHeader);

        // Topological sort
        var allNodes = _insightTreeDefinition.AllNodes.ToList();
        var sorted = new List<InsightTreeNode>();
        var visited = new HashSet<string>();

        void Visit(InsightTreeNode node)
        {
            if (visited.Contains(node.NodeId)) return;
            visited.Add(node.NodeId);
            if (node.Prerequisites != null)
                foreach (var prereq in node.Prerequisites)
                {
                    var prereqNode = allNodes.FirstOrDefault(n => n.NodeId == prereq.NodeId);
                    if (prereqNode != null) Visit(prereqNode);
                }
            sorted.Add(node);
        }
        foreach (var node in allNodes) Visit(node);

        foreach (var node in sorted)
        {
            bool unlocked = tree?.IsUnlocked(node.NodeId) ?? false;

            bool prereqsMet = true;
            if (node.Prerequisites != null)
                foreach (var prereq in node.Prerequisites)
                    if (!(tree?.IsUnlocked(prereq.NodeId) ?? false))
                    { prereqsMet = false; break; }

            bool canAfford = sight >= node.Cost;
            bool purchasable = !unlocked && prereqsMet && canAfford;

            var row = new VisualElement();
            row.AddToClassList("tree-node-row");
            row.style.backgroundColor = unlocked
                ? new StyleColor(new Color(0.15f, 0.25f, 0.15f, 0.8f))
                : prereqsMet
                    ? new StyleColor(new Color(0.15f, 0.15f, 0.25f, 0.8f))
                    : new StyleColor(new Color(0.1f, 0.1f, 0.15f, 0.5f));

            var dot = new VisualElement();
            dot.AddToClassList("tree-node-dot");
            dot.style.backgroundColor = unlocked
                ? new StyleColor(new Color(0.3f, 0.8f, 0.3f))
                : prereqsMet
                    ? new StyleColor(new Color(0.5f, 0.5f, 0.8f))
                    : new StyleColor(new Color(0.3f, 0.3f, 0.35f));
            row.Add(dot);

            var info = new VisualElement();
            info.AddToClassList("tree-node-info");

            var nameLabel = new Label(node.DisplayName);
            nameLabel.AddToClassList("tree-node-name");
            nameLabel.style.fontSize = 13;
            nameLabel.style.color = unlocked
                ? new StyleColor(new Color(0.7f, 0.95f, 0.7f))
                : prereqsMet
                    ? new StyleColor(new Color(0.85f, 0.85f, 0.95f))
                    : new StyleColor(new Color(0.4f, 0.4f, 0.5f));
            info.Add(nameLabel);

            var descLabel = new Label(node.Description ?? node.FlagName);
            descLabel.AddToClassList("tree-node-desc");
            info.Add(descLabel);

            if (!prereqsMet && node.Prerequisites != null && node.Prerequisites.Length > 0)
            {
                var prereqLabel = new Label($"Requires: {string.Join(", ", System.Array.ConvertAll(node.Prerequisites, p => p.DisplayName))}");
                prereqLabel.AddToClassList("tree-node-prereq");
                info.Add(prereqLabel);
            }
            row.Add(info);

            if (unlocked)
            {
                var unlockedLabel = new Label("✓ Unlocked");
                unlockedLabel.AddToClassList("tree-node-unlocked-label");
                row.Add(unlockedLabel);
            }
            else if (purchasable)
            {
                var btn = new Button();
                btn.text = $"Unlock ({node.Cost} Sight)";
                btn.AddToClassList("tree-node-unlock-btn");
                string nodeId = node.NodeId;
                btn.clicked += () =>
                {
                    PlayerNetworkAgent.LocalAgent?.CmdUnlockNode(nodeId);
                };
                row.Add(btn);
            }
            else if (prereqsMet)
            {
                var costLabel = new Label($"{node.Cost} Sight");
                costLabel.AddToClassList("tree-node-cost-label");
                row.Add(costLabel);
            }

            _insightContent.Add(row);
        }
    }
    private void RefreshOwnBoard(List<BoardPermanent> board)
    {
        if (_ownBoardZone == null || _ownBoardZone.panel == null) return;

        _ownBoardZone.schedule.Execute(() =>
        {
            _ownBoardZone.Clear();

            if (board != null)
                foreach (var permanent in board)
                    _ownBoardZone.Add(MakePermanentChip(
                        permanent.DisplayName,
                        permanent.TurnsRemaining,
                        permanent.ProtectedThisTurn,
                        hidden: false,
                        permanent.InstanceId));

            var badge = _root.Q<Label>("own-board-count");
            if (badge != null)
            {
                badge.text = (board?.Count ?? 0) > 0 ? board.Count.ToString() : "";
                _ownBoardZone.Add(badge);
            }
        });
    }

    private void RefreshOpponentBoard(List<BoardPermanent> board)
    {
        if (_opponentBoardZone == null || _opponentBoardZone.panel == null) return;

        _opponentBoardZone.schedule.Execute(() =>
        {
            _opponentBoardZone.Clear();

            if (board != null)
                foreach (var permanent in board)
                {
                    bool isHidden = permanent.PermanentId == "hidden";
                    _opponentBoardZone.Add(MakePermanentChip(
                        isHidden ? "???" : permanent.DisplayName,
                        isHidden ? -2 : permanent.TurnsRemaining,
                        permanent.ProtectedThisTurn,
                        hidden: isHidden,
                        permanent.InstanceId));
                }

            var badge = _root.Q<Label>("opponent-board-count");
            if (badge != null)
            {
                badge.text = (board?.Count ?? 0) > 0 ? board.Count.ToString() : "";
                _opponentBoardZone.Add(badge);
            }
        });
    }

    private VisualElement MakePermanentChip(string name, int turnsRemaining, bool protected_, bool hidden, int instanceId = 0)
    {
        var chip = new VisualElement();
        chip.userData = instanceId;
        chip.AddToClassList("permanent-chip");
        if (hidden) chip.AddToClassList("permanent-chip-hidden");
        if (protected_) chip.AddToClassList("permanent-chip-protected");

        var nameLabel = new Label(name);
        nameLabel.AddToClassList("permanent-chip-name");
        chip.Add(nameLabel);

        string durationText = turnsRemaining == -1 ? "∞"
                            : turnsRemaining == -2 ? "?"
                            : $"{turnsRemaining}T";
        var durationLabel = new Label(durationText);
        durationLabel.AddToClassList("permanent-chip-duration");
        chip.Add(durationLabel);

        if (protected_)
        {
            var shieldLabel = new Label("Protected");
            shieldLabel.AddToClassList("permanent-chip-protected-label");
            chip.Add(shieldLabel);
        }

        // Tooltip
        string tooltipBody = hidden ? "Hidden permanent." : GetPermanentTooltip(name);
        chip.RegisterCallback<PointerEnterEvent>(evt =>
            TooltipController.Instance?.Show(name, tooltipBody, evt.position));
        chip.RegisterCallback<PointerLeaveEvent>(_ =>
            TooltipController.Instance?.Hide());

        return chip;
    }

    private string GetPermanentTooltip(string displayName) => displayName switch
    {
        "Cha Cha - Loyal Chupacabra" => "Outgoing attacks deal +1 damage. Duration: 2 turns.",
        "Cursed Goblet" => "Reduces incoming damage by 20%. Infinite duration.",
        "Totem of Sharpness" => "Attacks that cost a Dagger or were upcast deal +3 damage. Duration: 2 turns.",
        "Mirror of Moonlight" => "Copies all instants you play into your queue at speed 2. Duration: 2 turns.",
        "Totem of Sacrifice" => "Reduces blood costs by 1 (minimum 1). Infinite duration.",
        "Totem of Progress" => "Upcasted cards draw 1 card on resolution. Duration: 3 turns.",
        "Totem of Warding" => "When opponent deals damage to you, apply 1 Bleed to them. Duration: 2 turns.",
        _ => "No description available."
    };

    private void RefreshShopPanel(ClientGameStateView view)
    {
        bool isShop = view.CurrentPhase == TurnPhase.ShopPhase;
        _shopPanel.style.display = isShop ? DisplayStyle.Flex : DisplayStyle.None;
        if (!isShop) return;

        var shopDoneBtn = _root.Q<Button>("shop-done-btn");
        shopDoneBtn.SetEnabled(!view.ShopDoneSubmitted);
        _shopWaitingLabel.style.display = view.ShopDoneSubmitted ? DisplayStyle.Flex : DisplayStyle.None;



        var offer = view.OwnShopOffer;
        var own = view.OwnState;
        int silver = own.Silver;

        _shopSilverLabel.text = $"Silver: {silver}";

        _shopPowerRow.schedule.Execute(() =>
        {
            _shopPowerRow.Clear();
            _shopStrategyRow.Clear();
            _shopColorlessRow.Clear();
            _shopPassivesRow.Clear();
            _shopServicesRow.Clear();

            if (offer == null) return;

            // Power cards
            for (int i = 0; i < offer.PowerCards.Count; i++)
            {
                int idx = i;
                string name = offer.PowerDisplayNames != null && i < offer.PowerDisplayNames.Count
                    ? offer.PowerDisplayNames[i] : offer.PowerCards[i];
                int price = offer.PowerPrices[i];
                bool sold = price < 0;
                _shopPowerRow.Add(MakeShopCardItem(name, price, silver, sold, () =>
                    PlayerNetworkAgent.LocalAgent?.CmdShopPurchase(ShopPurchaseType.PowerCard, idx)));
            }

            // Strategy cards
            for (int i = 0; i < offer.StrategyCards.Count; i++)
            {
                int idx = i;
                string name = offer.StrategyDisplayNames != null && i < offer.StrategyDisplayNames.Count
                    ? offer.StrategyDisplayNames[i] : offer.StrategyCards[i];
                int price = offer.StrategyPrices[i];
                bool sold = price < 0;
                _shopStrategyRow.Add(MakeShopCardItem(name, price, silver, sold, () =>
                    PlayerNetworkAgent.LocalAgent?.CmdShopPurchase(ShopPurchaseType.StrategyCard, idx)));
            }

            // Colorless cards
            for (int i = 0; i < offer.ColorlessCards.Count; i++)
            {
                int idx = i;
                string name = offer.ColorlessDisplayNames != null && i < offer.ColorlessDisplayNames.Count
                    ? offer.ColorlessDisplayNames[i] : offer.ColorlessCards[i];
                int price = offer.ColorlessPrices[i];
                bool sold = price < 0;
                _shopColorlessRow.Add(MakeShopCardItem(name, price, silver, sold, () =>
                    PlayerNetworkAgent.LocalAgent?.CmdShopPurchase(ShopPurchaseType.ColorlessCard, idx)));
            }

            // Passives
            for (int i = 0; i < offer.Passives.Count; i++)
            {
                int idx = i;
                string name = offer.PassiveDisplayNames != null && i < offer.PassiveDisplayNames.Count
                    ? offer.PassiveDisplayNames[i] : offer.Passives[i];
                int price = offer.PassivePrices[i];
                bool sold = price < 0;
                _shopPassivesRow.Add(MakeShopCardItem(name, price, silver, sold, () =>
                    PlayerNetworkAgent.LocalAgent?.CmdShopPurchase(ShopPurchaseType.Passive, idx)));
            }

            // Fixed service slots
            AddServiceSlot("HP Regen (Small)", "+25% HP", offer.HpRegenSmallCost, silver,
                () => PlayerNetworkAgent.LocalAgent?.CmdShopPurchase(ShopPurchaseType.HpRegenSmall, 0));
            AddServiceSlot("HP Regen (Large)", "+50% HP", offer.HpRegenLargeCost, silver,
                () => PlayerNetworkAgent.LocalAgent?.CmdShopPurchase(ShopPurchaseType.HpRegenLarge, 0));
            AddServiceSlot("Sight (Small)", "+2 Sight", offer.SightSmallCost, silver,
                () => PlayerNetworkAgent.LocalAgent?.CmdShopPurchase(ShopPurchaseType.SightSmall, 0));
            AddServiceSlot("Sight (Large)", "+5 Sight", offer.SightLargeCost, silver,
                () => PlayerNetworkAgent.LocalAgent?.CmdShopPurchase(ShopPurchaseType.SightLarge, 0));
            AddServiceSlot("Per Turn Resource", "+1 Per Turn", offer.PerTurnResourceCost, silver,
                () => PlayerNetworkAgent.LocalAgent?.CmdShopPurchase(ShopPurchaseType.PerTurnResource, 0));
        });
    }

    private VisualElement MakeShopCardItem(string name, int price, int silver, bool sold, System.Action onBuy, CardInstanceView data = null)
    {
        var item = new VisualElement();
        item.AddToClassList("shop-item");
        if (sold) item.AddToClassList("shop-item-sold");

        var nameLabel = new Label(name);
        nameLabel.AddToClassList("shop-item-name");
        item.Add(nameLabel);

        if (sold)
        {
            var soldLabel = new Label("SOLD");
            soldLabel.AddToClassList("shop-item-price");
            item.Add(soldLabel);
        }
        else
        {
            var priceLabel = new Label($"{price} Silver");
            priceLabel.AddToClassList("shop-item-price");
            if (silver < price) priceLabel.AddToClassList("shop-item-cant-afford");
            item.Add(priceLabel);

            var btn = new Button();
            btn.AddToClassList("shop-buy-btn");
            btn.text = "Buy";
            btn.SetEnabled(silver >= price);
            btn.clicked += onBuy;
            item.Add(btn);
        }

        if (data != null)
        {
            string body = GetCardTooltipBody(data.CardId, data.Type, data.ModifiedSpeed);
            item.RegisterCallback<PointerEnterEvent>(evt =>
                TooltipController.Instance?.Show(name, body, evt.position));
            item.RegisterCallback<PointerLeaveEvent>(_ => TooltipController.Instance?.Hide());
        }

        return item;
    }

    private void AddServiceSlot(string name, string desc, int price, int silver, System.Action onBuy)
    {
        var item = new VisualElement();
        item.AddToClassList("shop-item");
        if (price < 0) item.AddToClassList("shop-item-sold");

        var nameLabel = new Label(name);
        nameLabel.AddToClassList("shop-item-name");
        item.Add(nameLabel);

        var descLabel = new Label(desc);
        descLabel.AddToClassList("shop-item-desc");
        item.Add(descLabel);

        if (price < 0)
        {
            var soldLabel = new Label("SOLD");
            soldLabel.AddToClassList("shop-item-price");
            item.Add(soldLabel);
        }
        else
        {
            var priceLabel = new Label($"{price} Silver");
            priceLabel.AddToClassList("shop-item-price");
            if (silver < price) priceLabel.AddToClassList("shop-item-cant-afford");
            item.Add(priceLabel);

            var btn = new Button();
            btn.AddToClassList("shop-buy-btn");
            btn.text = "Buy";
            btn.SetEnabled(silver >= price);
            btn.clicked += onBuy;
            item.Add(btn);
        }

        _shopServicesRow.Add(item);
    }

    private void RefreshAuctionPanel(ClientGameStateView view)
    {
        bool isAuction = view.CurrentPhase == TurnPhase.AuctionPhase;
        _auctionPanel.style.display = isAuction ? DisplayStyle.Flex : DisplayStyle.None;
        if (!isAuction)
        {
            _currentBids = new int[] { 0, 0, 0 };
            return;
        }

        var offer = view.CurrentAuctionOffer;
        var own = view.OwnState;
        _auctionSilverLabel.text = $"Silver: {own.Silver}";

        bool submitted = view.AuctionBidsSubmitted;
        _auctionSubmitBtn.style.display = submitted ? DisplayStyle.None : DisplayStyle.Flex;
        _auctionWaitingLabel.style.display = submitted ? DisplayStyle.Flex : DisplayStyle.None;

        if (submitted) return;

        _auctionCardsRow.schedule.Execute(() =>
        {
            _auctionCardsRow.Clear();
            if (offer?.CardIds == null) return;

            for (int i = 0; i < offer.CardIds.Count; i++)
            {
                int idx = i;
                string name = offer.CardDisplayNames != null && i < offer.CardDisplayNames.Count
                    ? offer.CardDisplayNames[i] : offer.CardIds[i];

                var slot = new VisualElement();
                slot.AddToClassList("auction-card-slot");

                var nameLabel = new Label(name);
                nameLabel.AddToClassList("auction-card-name");
                slot.Add(nameLabel);

                var bidLabel = new Label("Your Bid:");
                bidLabel.AddToClassList("auction-bid-label");
                slot.Add(bidLabel);

                // Bid controls — minus button, value display, plus button
                var controls = new VisualElement();
                controls.AddToClassList("auction-bid-controls");

                var minusBtn = new Button();
                minusBtn.AddToClassList("auction-bid-btn");
                minusBtn.text = "−";

                var bidValue = new Label(_currentBids[idx].ToString());
                bidValue.AddToClassList("auction-bid-value");

                var plusBtn = new Button();
                plusBtn.AddToClassList("auction-bid-btn");
                plusBtn.text = "+";

                minusBtn.clicked += () =>
                {
                    if (_currentBids[idx] > 0)
                    {
                        _currentBids[idx]--;
                        bidValue.text = _currentBids[idx].ToString();
                    }
                };

                plusBtn.clicked += () =>
                {
                    var current = ClientStateManager.Instance?.CurrentState?.OwnState;
                    int maxBid = current?.Silver ?? 0;
                    if (_currentBids[idx] < maxBid)
                    {
                        _currentBids[idx]++;
                        bidValue.text = _currentBids[idx].ToString();
                    }
                };

                controls.Add(minusBtn);
                controls.Add(bidValue);
                controls.Add(plusBtn);
                slot.Add(controls);

                _auctionCardsRow.Add(slot);
            }
        });
    }

    private void OnAuctionSubmit()
    {
        PlayerNetworkAgent.LocalAgent?.CmdSubmitAuctionBids(
            _currentBids[0], _currentBids[1], _currentBids[2]);
    }
    private void RefreshEventPanel(ClientGameStateView view)
    {
        bool isEvent = view.CurrentPhase == TurnPhase.EventPhase;
        _eventPanel.style.display = isEvent ? DisplayStyle.Flex : DisplayStyle.None;
        if (!isEvent) { _fortuneOwnChoice = null; return; }

        string eventId = view.CurrentEventId;
        if (string.IsNullOrEmpty(eventId)) return;

        _eventTitleLabel.text = FormatEventName(eventId);

        bool submitted = view.OwnChoiceSubmitted;
        _eventWaitingLabel.style.display = submitted ? DisplayStyle.Flex : DisplayStyle.None;
        if (submitted)
        {
            if (view.CurrentEventId == "fortune_favors_the_bold" && view.EventOutcome != null)
                BuildFortuneResultUI(view);  // show outcome instead of generic waiting
            else
                _eventWaitingLabel.style.display = DisplayStyle.Flex;
            return;
        }

        _eventContent.schedule.Execute(() =>
        {
            _eventContent.Clear();
            switch (eventId)
            {
                case "chopping_block": BuildChoppingBlockUI(view); break;
                case "blessing_in_disguise": BuildBlessingInDisguiseUI(view); break;
                case "calm_before_the_storm": break; // non-interactive, no UI needed
                case "too_good_to_be_true": BuildTooGoodToBeTrueUI(view); break;
                case "deal_with_the_devil": BuildDealWithTheDevilUI(view); break;
                case "writing_on_the_wall": BuildWritingOnTheWallUI(view); break;
                case "fortune_favors_the_bold": BuildFortuneUI(view); break;
            }
        });
    }

    private string FormatEventName(string id) =>
        string.Concat(id.Split('_').Select(p => char.ToUpper(p[0]) + p.Substring(1) + " ")).Trim();

    private void BuildChoppingBlockUI(ClientGameStateView view)
    {
        _eventDescLabel.text = "Choose up to 2 cards to permanently remove from your deck.";
        var selected = new HashSet<int>();
        var allCards = new List<CardInstanceView>();
        if (view.OwnState.Deck != null) allCards.AddRange(view.OwnState.Deck);
        if (view.OwnState.Discard != null) allCards.AddRange(view.OwnState.Discard);

        var cardRow = new VisualElement();
        cardRow.style.flexDirection = FlexDirection.Row;
        cardRow.style.flexWrap = Wrap.Wrap;
        _eventContent.Add(cardRow);

        foreach (var card in allCards)
        {
            int id = card.InstanceId;
            var item = MakeShopCardItem(card.DisplayName ?? card.CardId, 0, 999, false, () => { }, card);
            item.style.opacity = 1f;

            // Replace buy button with toggle select
            var btn = item.Q<Button>();
            if (btn != null) item.Remove(btn);

            var selectBtn = new Button();
            selectBtn.AddToClassList("shop-buy-btn");
            selectBtn.text = "Remove";
            selectBtn.clicked += () =>
            {
                if (selected.Contains(id))
                {
                    selected.Remove(id);
                    selectBtn.text = "Remove";
                    item.style.borderTopColor = new StyleColor(new Color(0.3f, 0.3f, 0.5f));
                }
                else if (selected.Count < 2)
                {
                    selected.Add(id);
                    selectBtn.text = "Removing";
                    item.style.borderTopColor = new StyleColor(new Color(0.8f, 0.3f, 0.3f));
                }
            };
            item.Add(selectBtn);
            cardRow.Add(item);
        }

        var confirmBtn = new Button();
        confirmBtn.AddToClassList("shop-done-btn");
        confirmBtn.text = "Confirm";
        confirmBtn.clicked += () =>
        {
            string choice = selected.Count > 0 ? string.Join(",", selected) : "";
            PlayerNetworkAgent.LocalAgent?.CmdSubmitEventChoice(choice);
        };
        _eventContent.Add(confirmBtn);
    }

    private void BuildBlessingInDisguiseUI(ClientGameStateView view)
    {
        _eventDescLabel.text = "Choose a blessing to grant your opponent.";

        var blessings = new[]
        {
        ("blessing_of_valor",     "Blessing of Valor",    "+1 to all attacks (permanent)"),
        ("blessing_of_clarity",   "Blessing of Clarity",  "+3 draw next turn"),
        ("blessing_of_grace",     "Blessing of Grace",    "+2 speed to all queued cards (permanent)"),
        ("blessing_of_fortitude", "Blessing of Fortitude","+3 shield each turn start (permanent)"),
    };

        foreach (var (id, name, desc) in blessings)
        {
            string choiceId = id;
            var item = new VisualElement();
            item.AddToClassList("shop-item");
            item.style.minWidth = 160;

            var nameLabel = new Label(name);
            nameLabel.AddToClassList("shop-item-name");
            item.Add(nameLabel);

            var descLabel = new Label(desc);
            descLabel.AddToClassList("shop-item-desc");
            item.Add(descLabel);

            var btn = new Button();
            btn.AddToClassList("shop-buy-btn");
            btn.text = "Grant";
            btn.clicked += () => PlayerNetworkAgent.LocalAgent?.CmdSubmitEventChoice(choiceId);
            item.Add(btn);

            _eventContent.Add(item);
        }
    }

    private void BuildTooGoodToBeTrueUI(ClientGameStateView view)
    {
        _eventDescLabel.text = "Choose your reward.";

        var options = new[] { ("silver", "10 Silver", "Spend at shops and auctions"),
                          ("sight",  "2 Sight",   "Spend on the INSIGHT tree") };
        foreach (var (id, name, desc) in options)
        {
            string choiceId = id;
            var item = new VisualElement();
            item.AddToClassList("shop-item");
            item.style.minWidth = 160;

            var nameLabel = new Label(name);
            nameLabel.AddToClassList("shop-item-name");
            item.Add(nameLabel);

            var descLabel = new Label(desc);
            descLabel.AddToClassList("shop-item-desc");
            item.Add(descLabel);

            var btn = new Button();
            btn.AddToClassList("shop-buy-btn");
            btn.text = "Choose";
            btn.clicked += () => PlayerNetworkAgent.LocalAgent?.CmdSubmitEventChoice(choiceId);
            item.Add(btn);

            _eventContent.Add(item);
        }
    }

    private void BuildDealWithTheDevilUI(ClientGameStateView view)
    {
        int currentHP = view.OwnState.HP;
        int lossAmount = Mathf.CeilToInt(currentHP / 2f);
        int remaining = Mathf.Max(1, currentHP - lossAmount);

        _eventDescLabel.text = $"Lose half your HP ({currentHP} → {remaining}) in exchange for permanent power.";

        var dealItem = new VisualElement();
        dealItem.AddToClassList("shop-item");
        dealItem.style.minWidth = 200;

        var dealName = new Label("Take the Deal");
        dealName.AddToClassList("shop-item-name");
        dealItem.Add(dealName);

        var dealDesc = new Label("Pact of the Devil:\n+1 damage on attacks\n+1 shield per turn\n+1 draw per turn");
        dealDesc.AddToClassList("shop-item-desc");
        dealItem.Add(dealDesc);

        var dealBtn = new Button();
        dealBtn.AddToClassList("shop-buy-btn");
        dealBtn.text = "Accept";
        dealBtn.clicked += () => PlayerNetworkAgent.LocalAgent?.CmdSubmitEventChoice("deal");
        dealItem.Add(dealBtn);
        _eventContent.Add(dealItem);

        var passItem = new VisualElement();
        passItem.AddToClassList("shop-item");
        passItem.style.minWidth = 200;

        var passName = new Label("Decline");
        passName.AddToClassList("shop-item-name");
        passItem.Add(passName);

        var passDesc = new Label("Keep your current HP.\nNo reward.");
        passDesc.AddToClassList("shop-item-desc");
        passItem.Add(passDesc);

        var passBtn = new Button();
        passBtn.AddToClassList("shop-buy-btn");
        passBtn.text = "Decline";
        passBtn.clicked += () => PlayerNetworkAgent.LocalAgent?.CmdSubmitEventChoice("pass");
        passItem.Add(passBtn);
        _eventContent.Add(passItem);
    }

    private void BuildWritingOnTheWallUI(ClientGameStateView view)
    {
        _eventDescLabel.text = "Your top 5 cards are revealed. Choose any number to discard.";
        var selected = new HashSet<int>();
        var cards = view.EventRevealedCards;

        if (cards == null || cards.Count == 0)
        {
            _eventContent.Add(new Label("No cards to reveal.") { });
            return;
        }

        var cardRow = new VisualElement();
        cardRow.style.flexDirection = FlexDirection.Row;
        cardRow.style.flexWrap = Wrap.Wrap;
        _eventContent.Add(cardRow);

        foreach (var card in cards)
        {
            int id = card.InstanceId;
            var item = MakeShopCardItem(card.DisplayName ?? card.CardId, 0, 999, false, () => { });
            var btn = item.Q<Button>();
            if (btn != null) item.Remove(btn);

            var selectBtn = new Button();
            selectBtn.AddToClassList("shop-buy-btn");
            selectBtn.text = "Discard";
            selectBtn.clicked += () =>
            {
                if (selected.Contains(id))
                {
                    selected.Remove(id);
                    selectBtn.text = "Discard";
                }
                else
                {
                    selected.Add(id);
                    selectBtn.text = "✓ Discarding";
                }
            };
            item.Add(selectBtn);
            cardRow.Add(item);
        }

        var confirmBtn = new Button();
        confirmBtn.AddToClassList("shop-done-btn");
        confirmBtn.text = "Confirm";
        confirmBtn.clicked += () =>
        {
            string choice = selected.Count > 0 ? string.Join(",", selected) : "";
            PlayerNetworkAgent.LocalAgent?.CmdSubmitEventChoice(choice);
        };
        _eventContent.Add(confirmBtn);
    }

    private void BuildFortuneUI(ClientGameStateView view)
    {
        _eventDescLabel.text = "Choose your wager and color. The wheel decides all.";

        // Amount buttons
        var amountLabel = new Label("Wager Amount:");
        amountLabel.AddToClassList("shop-item-name");
        _eventContent.Add(amountLabel);

        var amountRow = new VisualElement();
        amountRow.style.flexDirection = FlexDirection.Row;
        amountRow.style.marginBottom = 8;
        _eventContent.Add(amountRow);

        int selectedAmount = 5;
        Button activeAmountBtn = null;

        var amounts = new[] { ("5", "5 HP"), ("10", "10 HP"), ("20", "20 HP"), ("50", "50 HP") };
        var amountBtns = new List<Button>();
        foreach (var (id, label) in amounts)
        {
            string amountId = id;
            var btn = new Button();
            btn.AddToClassList("shop-buy-btn");
            btn.text = label;
            btn.clicked += () =>
            {
                selectedAmount = int.Parse(amountId);
                foreach (var b in amountBtns) b.RemoveFromClassList("fortune-selected");
                btn.AddToClassList("fortune-selected");
                activeAmountBtn = btn;
            };
            amountBtns.Add(btn);
            amountRow.Add(btn);
        }
        // Default select 5
        amountBtns[0].AddToClassList("fortune-selected");

        // Color buttons
        var colorLabel = new Label("Choose Color:");
        colorLabel.AddToClassList("shop-item-name");
        _eventContent.Add(colorLabel);

        var colorRow = new VisualElement();
        colorRow.style.flexDirection = FlexDirection.Row;
        colorRow.style.marginBottom = 12;
        _eventContent.Add(colorRow);

        string selectedColor = "red";
        var colorBtns = new List<Button>();

        foreach (var color in new[] { "red", "black" })
        {
            string c = color;
            var btn = new Button();
            btn.AddToClassList("shop-buy-btn");
            btn.AddToClassList($"fortune-color-{c}");
            btn.text = char.ToUpper(c[0]) + c.Substring(1);
            btn.clicked += () =>
            {
                selectedColor = c;
                foreach (var b in colorBtns) b.RemoveFromClassList("fortune-selected");
                btn.AddToClassList("fortune-selected");
            };
            colorBtns.Add(btn);
            colorRow.Add(btn);
        }
        colorBtns[0].AddToClassList("fortune-selected");

        var confirmBtn = new Button();
        confirmBtn.AddToClassList("shop-done-btn");
        confirmBtn.text = "Lock In";
        confirmBtn.clicked += () =>
        {
            string choice = $"{selectedAmount}:{selectedColor}";
            _fortuneOwnChoice = choice;
            PlayerNetworkAgent.LocalAgent?.CmdSubmitEventChoice(choice);
        };
        _eventContent.Add(confirmBtn);
    }
    private void BuildFortuneResultUI(ClientGameStateView view)
    {
        _eventWaitingLabel.style.display = DisplayStyle.None;
        _eventContent.schedule.Execute(() =>
        {
            _eventContent.Clear();

            // Wheel result display
            string outcome = view.EventOutcome;
            var resultLabel = new Label($"The wheel lands on... {outcome.ToUpper()}!");
            resultLabel.AddToClassList($"fortune-result-{outcome}");
            resultLabel.style.fontSize = 20;
            resultLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            resultLabel.style.marginBottom = 12;
            _eventContent.Add(resultLabel);

            // Show each player's outcome
            var ownChoice = view.OwnState != null
                ? ParseFortuneChoice(view)
                : null;
            if (ownChoice.HasValue)
            {
                bool won = ownChoice.Value.color == outcome;
                var outcomeLabel = new Label(won
                    ? $"You bet {ownChoice.Value.color.ToUpper()} for {ownChoice.Value.amount} HP — YOU WIN!"
                    : $"You bet {ownChoice.Value.color.ToUpper()} for {ownChoice.Value.amount} HP — YOU LOSE.");
                outcomeLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                outcomeLabel.style.color = won
                    ? new StyleColor(new Color(0.4f, 0.9f, 0.4f))
                    : new StyleColor(new Color(0.9f, 0.3f, 0.3f));
                _eventContent.Add(outcomeLabel);
            }
        });
    }

    private (int amount, string color)? ParseFortuneChoice(ClientGameStateView view)
    {
        if (string.IsNullOrEmpty(_fortuneOwnChoice)) return null;
        var parts = _fortuneOwnChoice.Split(':');
        if (parts.Length != 2) return null;
        return (int.Parse(parts[0]), parts[1]);
    }

    private void ShowGameOverScreen(ClientGameStateView view)
    {
        // Reuse the pile overlay as a simple modal — it's already a full-screen dark overlay
        _pileOverlay.style.display = DisplayStyle.Flex;

        // Hide the close button so they can't dismiss it
        _root.Q<Button>("pile-overlay-close").style.display = DisplayStyle.None;

        _pileOverlayTitle.text = view.WinnerPlayerId == view.OwnState?.PlayerId
            ? "Victory"
            : "Defeat";

        _pileContent.schedule.Execute(() =>
        {
            _pileContent.Clear();

            var msg = new Label(view.WinnerPlayerId == view.OwnState?.PlayerId
                ? "You have won the match."
                : "You have been defeated.");
            msg.style.fontSize = 16;
            msg.style.unityTextAlign = TextAnchor.MiddleCenter;
            msg.style.color = new StyleColor(new Color(0.85f, 0.85f, 0.9f));
            msg.style.marginTop = 20;
            _pileContent.Add(msg);
        });
    }

    private void RefreshPassivesShelf(PlayerStateView own, ClientGameStateView view)
    {
        _passivesShelf.Clear();
        if (own?.Passives == null) return;

        foreach (var passive in own.Passives)
        {
            bool isInteractive = IsInteractivePassive(passive.PassiveId);
            bool isExhausted = passive.IsExhausted;
            bool unavailable = passive.PassiveId == "slight_of_hand" && view.OwnState != null &&
                               (view.CurrentPhase != TurnPhase.MainPhase);

            var chip = new VisualElement();
            chip.AddToClassList("passive-chip");
            if (isExhausted) chip.AddToClassList("passive-chip-exhausted");
            if (!isInteractive) chip.AddToClassList("passive-chip-passive");

            var dot = new VisualElement();
            dot.AddToClassList("passive-chip-dot");
            chip.Add(dot);

            var label = new Label(passive.DisplayName);
            label.AddToClassList("passive-chip-label");
            chip.Add(label);

            // Tooltip
            var passiveDef = Resources.Load<PassiveDefinition>($"Passives/{passive.PassiveId}");
            string tooltipBody = passiveDef != null ? passiveDef.Description : GetPassiveDescription(passive.PassiveId);
            chip.RegisterCallback<PointerEnterEvent>(evt =>
                TooltipController.Instance?.Show(passive.DisplayName, tooltipBody, evt.position));
            chip.RegisterCallback<PointerLeaveEvent>(_ =>
                TooltipController.Instance?.Hide());



            if (passive.StackCount > 0)
            {
                var stackLabel = new Label($"x{passive.StackCount}");
                stackLabel.AddToClassList("passive-chip-stack");
                chip.Add(stackLabel);
            }

            if (isInteractive && !isExhausted && !unavailable)
                chip.RegisterCallback<ClickEvent>(_ => OnPassiveChipClicked(passive));

            _passivesShelf.Add(chip);
        }

        // ── Status effects (debug display) ──────────────────────────
        if (own?.StatusEffects == null) return;

        foreach (var effect in own.StatusEffects)
        {
            var chip = new VisualElement();
            chip.AddToClassList("passive-chip");
            chip.AddToClassList("status-effect-chip");

            var dot = new VisualElement();
            dot.AddToClassList("passive-chip-dot");
            dot.AddToClassList($"status-dot-{effect.EffectId}");
            chip.Add(dot);

            string displayName = effect.EffectId switch
            {
                "bleed" => "Bleed",
                "shield_lock" => "Shield Lock",
                "devilbound" => "Devilbound",
                _ => effect.EffectId
            };

            string durationText = effect.Duration == -1 ? "∞" : $"{effect.Duration}t";

            var label = new Label($"{displayName} {effect.Value} ({durationText})");
            label.AddToClassList("passive-chip-label");
            chip.Add(label);

            chip.RegisterCallback<PointerEnterEvent>(evt =>
                TooltipController.Instance?.Show(displayName,
                    $"Value: {effect.Value}\nDuration: {durationText}", evt.position));
            chip.RegisterCallback<PointerLeaveEvent>(_ => TooltipController.Instance?.Hide());

            _passivesShelf.Add(chip);
        }
    }

    private bool IsInteractivePassive(string passiveId) => passiveId switch
    {
        "slight_of_hand" => true,
        "market_crash" => true,
        "blessed_diary" => true,
        "ancient_telescope" => true,
        _ => false
    };

    private void OnPassiveChipClicked(Passive passive)
    {
        switch (passive.PassiveId)
        {
            case "slight_of_hand":
                OpenSlightOfHandMenu();
                break;
            case "market_crash":
                PlayerNetworkAgent.LocalAgent?.CmdMarketCrash();
                break;
            case "blessed_diary":
                OpenBlessedDiaryMenu();
                break;
            case "ancient_telescope":
                OpenAncientTelescopeMenu();
                break;
        }
    }

    private void OpenSlightOfHandMenu()
    {
        var view = ClientStateManager.Instance?.CurrentState;
        if (view == null) return;

        var discard = view.OwnState?.Discard;

        _pileOverlayTitle.text = "Slight of Hand — Choose a card to return to deck";
        _pileOverlay.style.display = DisplayStyle.Flex;

        _pileContent.schedule.Execute(() =>
        {
            _pileContent.Clear();

            if (discard == null || discard.Count == 0)
            {
                var msg = new Label("Your discard pile is empty.");
                msg.AddToClassList("overlay-locked-label");
                _pileContent.Add(msg);
                return;
            }

            foreach (var card in discard)
            {
                var btn = new Button();
                btn.AddToClassList("shop-buy-btn");
                btn.text = card.DisplayName ?? card.CardId;

                string tooltipBody = GetCardTooltipBody(card.CardId, card.Type, card.ModifiedSpeed);
                btn.RegisterCallback<PointerEnterEvent>(evt =>
                    TooltipController.Instance?.Show(card.DisplayName ?? card.CardId, tooltipBody, evt.position));
                btn.RegisterCallback<PointerLeaveEvent>(_ => TooltipController.Instance?.Hide());

                int capturedId = card.InstanceId;
                btn.clicked += () =>
                {
                    _pileOverlay.style.display = DisplayStyle.None;
                    TooltipController.Instance?.Hide();
                    PlayerNetworkAgent.LocalAgent?.CmdSlightOfHand(capturedId);
                };
                _pileContent.Add(btn);
            }
        });
    }
    private void OpenBlessedDiaryMenu()
    {
        var view = ClientStateManager.Instance?.CurrentState;
        if (view == null) return;

        var board = view.OwnState?.Board;

        _pileOverlayTitle.text = "Blessed Diary — Choose a permanent to protect";
        _pileOverlay.style.display = DisplayStyle.Flex;

        _pileContent.schedule.Execute(() =>
        {
            _pileContent.Clear();

            if (board == null || board.Count == 0)
            {
                var msg = new Label("No permanents on your board.");
                msg.AddToClassList("overlay-locked-label");
                _pileContent.Add(msg);
                return;
            }

            // Show current attachment if any
            var diary = view.OwnState?.Passives?.Find(p => p.PassiveId == "blessed_diary");
            if (diary != null && !string.IsNullOrEmpty(diary.TargetPermanentId))
            {
                var currentLabel = new Label($"Currently attached to: {diary.TargetPermanentId}");
                currentLabel.AddToClassList("overlay-locked-label");
                _pileContent.Add(currentLabel);
            }

            foreach (var permanent in board)
            {
                var btn = new Button();
                btn.AddToClassList("shop-buy-btn");
                btn.text = permanent.DisplayName;

                string tooltipBody = GetPermanentTooltip(permanent.DisplayName);
                btn.RegisterCallback<PointerEnterEvent>(evt =>
                    TooltipController.Instance?.Show(permanent.DisplayName, tooltipBody, evt.position));
                btn.RegisterCallback<PointerLeaveEvent>(_ => TooltipController.Instance?.Hide());

                string capturedId = permanent.PermanentId;
                btn.clicked += () =>
                {
                    _pileOverlay.style.display = DisplayStyle.None;
                    TooltipController.Instance?.Hide();
                    PlayerNetworkAgent.LocalAgent?.CmdBlessedDiaryTarget(capturedId);
                };
                _pileContent.Add(btn);
            }
        });
    }
    private void OpenAncientTelescopeMenu()
    {
        var view = ClientStateManager.Instance?.CurrentState;
        var def = GetInsightTreeDef();
        if (view == null || def == null) return;

        var own = view.OwnState;
        var insightTree = own?.InsightTree;

        _pileOverlayTitle.text = "Ancient Telescope";
        _pileOverlay.style.display = DisplayStyle.Flex;

        _pileContent.schedule.Execute(() =>
        {
            _pileContent.Clear();

            bool anyAvailable = false;
            foreach (var node in def.AllNodes)
            {
                bool unlocked = insightTree?.IsUnlocked(node.NodeId) ?? false;
                if (unlocked) continue;

                bool prereqsMet = true;
                if (node.Prerequisites != null)
                    foreach (var prereq in node.Prerequisites)
                        if (!(insightTree?.IsUnlocked(prereq.NodeId) ?? false))
                        { prereqsMet = false; break; }

                if (!prereqsMet) continue;

                anyAvailable = true;
                var btn = new Button();
                btn.AddToClassList("shop-buy-btn");
                btn.text = node.DisplayName;
                btn.clicked += () =>
                {
                    _pileOverlay.style.display = DisplayStyle.None;
                    PlayerNetworkAgent.LocalAgent?.CmdAncientTelescope(node.NodeId);
                };
                _pileContent.Add(btn);
            }

            if (!anyAvailable)
            {
                var msg = new Label("No nodes available to reveal.");
                msg.AddToClassList("overlay-locked-label");
                _pileContent.Add(msg);
            }
        });
    }

    public void BeginTargeting(
        List<BoardPermanent> targets,  // valid targets to highlight
        bool targetOwnBoard,           // true = own board, false = opponent board
        System.Action<int> onSelected,
        System.Action onCancelled)
    {
        _targetingActive = true;
        _onTargetSelected = onSelected;
        _onTargetCancelled = onCancelled;
        _highlightedChips.Clear();

        var zone = targetOwnBoard ? _ownBoardZone : _opponentBoardZone;

        // Highlight valid target chips
        zone.schedule.Execute(() =>
        {
            foreach (var child in zone.Children())
            {
                // Match chip to target by userData InstanceId we'll store on the chip
                if (child.userData is int instanceId &&
                    targets.Exists(t => t.InstanceId == instanceId))
                {
                    child.AddToClassList("permanent-chip-targeted");
                    int captured = instanceId;
                    child.RegisterCallback<ClickEvent>(OnTargetChipClicked);
                    child.userData = captured;
                    _highlightedChips.Add(child);
                }
            }

            // Click anywhere on root to cancel
            _root.RegisterCallback<ClickEvent>(OnTargetCancelClicked);
        });
    }

    private void OnTargetChipClicked(ClickEvent evt)
    {
        if (!_targetingActive) return;
        if (evt.currentTarget is VisualElement chip && chip.userData is int instanceId)
        {
            EndTargeting();
            _onTargetSelected?.Invoke(instanceId);
            evt.StopPropagation();
        }
    }

    private void OnTargetCancelClicked(ClickEvent evt)
    {
        if (!_targetingActive) return;
        EndTargeting();
        _onTargetCancelled?.Invoke();
    }

    private void EndTargeting()
    {
        _targetingActive = false;
        foreach (var chip in _highlightedChips)
        {
            chip.RemoveFromClassList("permanent-chip-targeted");
            chip.UnregisterCallback<ClickEvent>(OnTargetChipClicked);
        }
        _highlightedChips.Clear();
        _root.UnregisterCallback<ClickEvent>(OnTargetCancelClicked);
    }

    private string GetCardTooltipBody(string cardId, CardType type, int speed)
    {
        var def = Resources.Load<CardDefinition>($"Cards/{CardIdToAssetName(cardId)}");
        string body = def?.FlavourText ?? "";
        if (type == CardType.Queueable && speed > 0)
            body += body.Length > 0 ? $"\n\nSPD {speed}" : $"SPD {speed}";
        return body;
    }

    private string CardIdToAssetName(string cardId)
    {
        var parts = cardId.Split('_');
        return string.Concat(System.Array.ConvertAll(parts,
            p => char.ToUpper(p[0]) + p.Substring(1)));
    }

    private string GetPassiveDescription(string passiveId) => passiveId switch
    {
        "cursed_dagger" => "Your daggers are cursed. You may upcast attacks with a dagger to give them lifesteal.",
        "pact_of_the_devil" => "At the start of each turn, gain 1 shield and draw 1 card. Your attacks do +1 damage.",
        "blessing_of_valor" => "Your queued cards deal 1 extra damage.",
        "blessing_of_clarity" => "On acquire: draw 3 cards at the start of your next turn.",
        "blessing_of_grace" => "Your queued cards gain +2 speed.",
        "blessing_of_fortitude" => "At the start of each turn, gain 3 shield.",
        "season_of_harvest" => "Permanently gain +1 per-turn resource.",
        "accelerator" => "Each card you queue this turn gives +10 speed to subsequent cards.",
        "slight_of_hand" => "Once per turn during Main Phase: choose a card from your discard and shuffle it into your deck.",
        "clutterstorm" => "The first time you take HP damage each turn, add a Clutterstorm card to the queue.",
        "unbroken_chain" => "All your attacks gain +1 damage for each consecutive turn you've owned the first resolving card in the merged queue.",
        "natures_eye" => "Reveals one random opponent fog flag each turn.",
        "market_crash" => "Once: queue a Market Crash card at speed 10.",
        "blessed_diary" => "Attach to a permanent. When that permanent is destroyed by the opponent, draw 3 cards.",
        "ancient_telescope" => "Once per turn: temporarily reveal an insight node you have the prerequisites for.",
        "ceremonial_dagger" => "Whenever you apply a status effect to your opponent, also apply 1 Bleed.",
        "cha_cha_lifelong_companion" => "Cha Cha stays on your board permanently instead of expiring.",
        _ => passiveId
    };

}