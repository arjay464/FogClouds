using UnityEngine;
using UnityEngine.UIElements;
using FogClouds;

public class OpponentCardView : VisualElement
{
    private VisualElement _cardRoot;

    private static VisualTreeAsset _cardViewTemplate;

    public static void SetTemplate(VisualTreeAsset template) => _cardViewTemplate = template;

    public OpponentCardView(CardInstanceView data)
    {
        _cardViewTemplate.CloneTree(this);
        _cardRoot = this.Q<VisualElement>("card-root");
        _cardRoot.AddToClassList("opponent-card");
        this.pickingMode = PickingMode.Ignore;
        Populate(data);
    }

    // Face-down constructor — no data needed
    public OpponentCardView()
    {
        _cardViewTemplate.CloneTree(this);
        _cardRoot = this.Q<VisualElement>("card-root");
        _cardRoot.AddToClassList("opponent-card");
        _cardRoot.AddToClassList("card-facedown");
        this.pickingMode = PickingMode.Ignore;

        // Hide all content
        this.Q<VisualElement>("card-header").style.display = DisplayStyle.None;
        this.Q<VisualElement>("card-footer").style.display = DisplayStyle.None;
        this.Q<VisualElement>("card-art").style.display = DisplayStyle.Flex;
    }

    private void Populate(CardInstanceView data)
    {
        this.Q<Label>("card-name").text = data.DisplayName ?? data.CardId;

        var daggerPips = this.Q<VisualElement>("dagger-pips");
        daggerPips.Clear();
        for (int i = 0; i < data.Cost.Daggers; i++)
        {
            var pip = new VisualElement();
            pip.AddToClassList("dagger-pip");
            daggerPips.Add(pip);
        }

        var bloodCost = this.Q<Label>("blood-cost");
        bloodCost.text = data.Cost.Blood > 0 ? $"{data.Cost.Blood}" : "";
        bloodCost.style.display = data.Cost.Blood > 0 ? DisplayStyle.Flex : DisplayStyle.None;

        var cardRoot = this.Q<VisualElement>("card-root");
        switch (data.Type)
        {
            case CardType.Queueable:
                cardRoot.AddToClassList("card-queueable");
                this.Q<VisualElement>("type-shape").AddToClassList("shape-queueable");
                this.Q<Label>("speed-label").text = $"SPD {data.ModifiedSpeed}";
                this.Q<Label>("speed-label").style.display = DisplayStyle.Flex;
                break;
            case CardType.Instant:
                cardRoot.AddToClassList("card-instant");
                this.Q<VisualElement>("type-shape").AddToClassList("shape-instant");
                this.Q<Label>("speed-label").style.display = DisplayStyle.None;
                break;
            case CardType.Permanent:
                cardRoot.AddToClassList("card-permanent");
                this.Q<VisualElement>("type-shape").AddToClassList("shape-permanent");
                this.Q<Label>("speed-label").style.display = DisplayStyle.None;
                break;
        }

        var def = CardLibrary.Instance.Get(data.CardId);
        this.Q<Label>("flavour-text").text = def != null ? def.FlavourText : "";
    }
}