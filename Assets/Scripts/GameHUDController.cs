using UnityEngine;
using UnityEngine.UIElements;
using FogClouds;
using System.Collections.Generic;

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
    private Label _ownBoardCount;

    // Queue
    private VisualElement _ownQueue;
    private VisualElement _mergedQueue;
    private VisualElement _roguelikePanel;
    private VisualElement _categoryButtons;
    private VisualElement _cardOfferButtons;
    private Label _roguelikeWaiting;

    // Buttons
    private Button _endTurnButton;
    private Button _insightButton;
    private Button _passiveButton;
    // Roguelike panel

    private Dictionary<string, CharacterData> _characterCache = new();

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
        _ownBoardCount = _root.Q<Label>("own-board-count");
        _ownQueue = _root.Q<VisualElement>("own-queue");
        _mergedQueue = _root.Q<VisualElement>("merged-queue");
        _roguelikePanel = _root.Q<VisualElement>("roguelike-panel");
        _categoryButtons = _root.Q<VisualElement>("category-buttons");
        _cardOfferButtons = _root.Q<VisualElement>("card-offer-buttons");
        _roguelikeWaiting = _root.Q<Label>("roguelike-waiting");
        _endTurnButton = _root.Q<Button>("end-turn-button");
        _insightButton = _root.Q<Button>("insight-button");
        _passiveButton = _root.Q<Button>("passive-button");

        _endTurnButton.clicked += OnEndTurnClicked;
        _insightButton.clicked += OnInsightClicked;
        _passiveButton.clicked += OnPassiveClicked;

        _root.Q<Button>("power-btn").clicked += () => PlayerNetworkAgent.LocalAgent?.CmdCommitPower();
        _root.Q<Button>("strategy-btn").clicked += () => PlayerNetworkAgent.LocalAgent?.CmdCommitStrategy();
        _root.Q<Button>("insight-btn-roguelike").clicked += () => PlayerNetworkAgent.LocalAgent?.CmdChooseInsight();

        var handController = GetComponent<HandController>();
        handController?.Initialize(_root);

        if (ClientStateManager.Instance != null)
            ClientStateManager.Instance.OnStateUpdated += Refresh;
        else
            Debug.LogWarning("[GameHUDController] ClientStateManager not found in Start.");
    }

    void OnDisable()
    {
        if (ClientStateManager.Instance != null)
            ClientStateManager.Instance.OnStateUpdated -= Refresh;
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

        // Board count badge
        int boardCount = own.Board?.Count ?? 0;
        _ownBoardCount.text = boardCount > 0 ? boardCount.ToString() : "";

        // Queue visibility
        // Replace the queue visibility block in DoRefresh with this:

        bool isMerged = view.CurrentPhase == TurnPhase.QueueResolution || view.CurrentPhase == TurnPhase.QueueMerge;
        _mergedQueue.style.display = isMerged ? DisplayStyle.Flex : DisplayStyle.None;
        _ownQueue.style.display = isMerged ? DisplayStyle.None : DisplayStyle.Flex;

        RefreshOwnQueue(view.OwnQueue);
        RefreshMergedQueue(view.MergedQueue, isMerged);

        // Button visibility
        _endTurnButton.style.display =
            view.CurrentPhase == TurnPhase.MainPhase ? DisplayStyle.Flex : DisplayStyle.None;

        RefreshRoguelikePanel(view);

        // Show passive button only if player has interactive passives (Phase 5+)
        _passiveButton.style.display = DisplayStyle.None;
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
        // Phase 6: open INSIGHT overlay
        Debug.Log("[GameHUDController] INSIGHT button clicked.");
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

                var nameLabel = new Label(entry.Card.DisplayName);
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

                var nameLabel = new Label(entry.Card.DisplayName);
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
        if (!isRoguelike) return;

        var own = view.OwnState;

        if (own.UpgradeChoiceSubmitted)
        {
            _categoryButtons.style.display = DisplayStyle.None;
            _cardOfferButtons.style.display = DisplayStyle.None;
            _roguelikeWaiting.style.display = DisplayStyle.Flex;
            return;
        }

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

            for (int i = 0; i < offerList.Count; i++)
            {
                int index = i;
                var def = Resources.Load<CardDefinition>($"Cards/{offerList[i]}");
                string displayName = def != null ? def.DisplayName : offerList[i];
                int price = priceList != null && i < priceList.Count ? priceList[i] : 0;

                var btn = new Button();
                btn.AddToClassList("card-offer-btn");
                btn.text = $"{displayName}\n{price} Sight";
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
}