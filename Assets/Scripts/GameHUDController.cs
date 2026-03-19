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

    // Buttons
    private Button _endTurnButton;
    private Button _insightButton;
    private Button _passiveButton;

    private Dictionary<string, CharacterData> _characterCache = new();

    void Awake()
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
        _endTurnButton = _root.Q<Button>("end-turn-button");
        _insightButton = _root.Q<Button>("insight-button");
        _passiveButton = _root.Q<Button>("passive-button");

        _endTurnButton.clicked += OnEndTurnClicked;
        _insightButton.clicked += OnInsightClicked;
        _passiveButton.clicked += OnPassiveClicked;
    }

    void OnEnable()
    {
        if (ClientStateManager.Instance != null)
            ClientStateManager.Instance.OnStateUpdated += Refresh;
    }

    void OnDisable()
    {
        if (ClientStateManager.Instance != null)
            ClientStateManager.Instance.OnStateUpdated -= Refresh;
    }

    private void Refresh(ClientGameStateView view)
    {
        var own = view.OwnState;
        if (own == null) return;

        // Phase and turn
        _phaseLabel.text = view.CurrentPhase.ToString();
        _turnLabel.text = $"Turn {view.TurnNumber}";

        // HP bar
        float hpPercent = own.HP / (float)LoadBaseHP(own.CharacterId);
        _hpBarFill.style.width = Length.Percent(hpPercent * 100f);
        _hpLabel.text = $"{own.HP}/{LoadBaseHP(own.CharacterId)}";

        // Shield
        _shieldLabel.text = own.Shield > 0 ? $"🛡 {own.Shield}" : "";

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
        bool isMerged = view.CurrentPhase == TurnPhase.QueueResolution;
        _mergedQueue.style.display = isMerged ? DisplayStyle.Flex : DisplayStyle.None;
        _ownQueue.style.display = isMerged ? DisplayStyle.None : DisplayStyle.Flex;

        // Button visibility
        _endTurnButton.style.display =
            view.CurrentPhase == TurnPhase.MainPhase ? DisplayStyle.Flex : DisplayStyle.None;

        // Show passive button only if player has interactive passives (Phase 5+)
        _passiveButton.style.display = DisplayStyle.None;
    }

    private void RefreshPips(ResourceState resources)
    {
        _perTurnPips.Clear();
        if (resources == null) return;

        for (int i = 0; i < resources.PerTurnResourceMax; i++)
        {
            var pip = new VisualElement();
            pip.AddToClassList(i < resources.PerTurnResource ? "pip-filled" : "pip-empty");
            _perTurnPips.Add(pip);
        }
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
}