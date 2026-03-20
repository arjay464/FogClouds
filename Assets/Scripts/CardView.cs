using UnityEngine;
using UnityEngine.UIElements;
using FogClouds;
using System;

public class CardView : VisualElement
{
    public CardInstanceView CardData { get; private set; }
    public bool IsUpcast { get; private set; } = false;
    public bool IsSelected { get; private set; } = false;

    public event Action<CardView> OnSelected;
    public event Action<CardView> OnUpcastToggled;

    private Label _nameLabel;
    private VisualElement _daggerPips;
    private Label _bloodCost;
    private VisualElement _typeShape;
    private Label _speedLabel;
    private Label _flavourLabel;

    public CardView(CardInstanceView data)
    {
        // Load and clone the UXML template
        var template = Resources.Load<VisualTreeAsset>("UI/CardView");
        template.CloneTree(this);

        _nameLabel = this.Q<Label>("card-name");
        _daggerPips = this.Q<VisualElement>("dagger-pips");
        _bloodCost = this.Q<Label>("blood-cost");
        _typeShape = this.Q<VisualElement>("type-shape");
        _speedLabel = this.Q<Label>("speed-label");
        _flavourLabel = this.Q<Label>("flavour-text");

        Populate(data);
        RegisterCallbacks();
    }

    public void Populate(CardInstanceView data)
    {
        CardData = data;

        _nameLabel.text = data.DisplayName ?? data.CardId;

        // Dagger pips
        _daggerPips.Clear();
        for (int i = 0; i < data.Cost.Daggers; i++)
        {
            var pip = new VisualElement();
            pip.AddToClassList("dagger-pip");
            _daggerPips.Add(pip);
        }

        // Blood cost
        _bloodCost.text = data.Cost.Blood > 0 ? $"{data.Cost.Blood}" : "";
        _bloodCost.style.display = data.Cost.Blood > 0
            ? DisplayStyle.Flex : DisplayStyle.None;

        // Type shape and card border variant
        var cardRoot = this.Q<VisualElement>("card-root");
        cardRoot.RemoveFromClassList("card-queueable");
        cardRoot.RemoveFromClassList("card-instant");
        cardRoot.RemoveFromClassList("card-permanent");
        _typeShape.RemoveFromClassList("shape-queueable");
        _typeShape.RemoveFromClassList("shape-instant");
        _typeShape.RemoveFromClassList("shape-permanent");

        switch (data.Type)
        {
            case CardType.Queueable:
                cardRoot.AddToClassList("card-queueable");
                _typeShape.AddToClassList("shape-queueable");
                _speedLabel.text = $"SPD {data.ModifiedSpeed}";
                _speedLabel.style.display = DisplayStyle.Flex;
                break;
            case CardType.Instant:
                cardRoot.AddToClassList("card-instant");
                _typeShape.AddToClassList("shape-instant");
                _speedLabel.style.display = DisplayStyle.None;
                break;
            case CardType.Permanent:
                cardRoot.AddToClassList("card-permanent");
                _typeShape.AddToClassList("shape-permanent");
                _speedLabel.style.display = DisplayStyle.None;
                break;
        }

        // Flavour text — load from CardDefinition asset
        var def = Resources.Load<CardDefinition>($"Cards/{data.CardId}");
        _flavourLabel.text = def != null ? def.FlavourText : "";
    }

    private void RegisterCallbacks()
    {
        // Left click — select
        RegisterCallback<ClickEvent>(evt =>
        {
            if (evt.button == 0)
            {
                SetSelected(!IsSelected);
                OnSelected?.Invoke(this);
            }
        });

        // Right click — toggle upcast
        RegisterCallback<ContextualMenuPopulateEvent>(evt => evt.StopPropagation());
        RegisterCallback<PointerDownEvent>(evt =>
        {
            if (evt.button == 1 && IsSelected)
            {
                ToggleUpcast();
                evt.StopPropagation();
            }
        });
    }

    public void SetSelected(bool selected)
    {
        IsSelected = selected;
        var cardRoot = this.Q<VisualElement>("card-root");
        if (selected)
            cardRoot.AddToClassList("card-selected");
        else
        {
            cardRoot.RemoveFromClassList("card-selected");
            // Deselecting clears upcast too
            SetUpcast(false);
        }
    }

    public void ToggleUpcast() => SetUpcast(!IsUpcast);

    public void SetUpcast(bool upcast)
    {
        IsUpcast = upcast;
        var cardRoot = this.Q<VisualElement>("card-root");
        if (upcast)
            cardRoot.AddToClassList("card-upcast");
        else
            cardRoot.RemoveFromClassList("card-upcast");
        OnUpcastToggled?.Invoke(this);
    }

    public void SetDragging(bool dragging)
    {
        var cardRoot = this.Q<VisualElement>("card-root");
        if (dragging)
            cardRoot.AddToClassList("card-dragging");
        else
            cardRoot.RemoveFromClassList("card-dragging");
    }
}