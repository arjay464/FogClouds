using UnityEngine;
using UnityEngine.UIElements;

public class TooltipController : MonoBehaviour
{
    public static TooltipController Instance { get; private set; }

    private VisualElement _tooltip;
    private Label _titleLabel;
    private Label _bodyLabel;
    private VisualElement _root;

    private const float Offset = 12f;

    public void Initialize(VisualElement root)
    {
        Instance = this;
        _root = root;
        _tooltip = root.Q<VisualElement>("tooltip-box");
        _titleLabel = root.Q<Label>("tooltip-title");
        _bodyLabel = root.Q<Label>("tooltip-body");
    }

    public void Show(string title, string body, Vector2 panelPosition)
    {
        if (_tooltip == null) return;
        _titleLabel.text = title;
        _bodyLabel.text = body;
        _tooltip.style.display = DisplayStyle.Flex;
        _tooltip.BringToFront();

        _tooltip.schedule.Execute(() =>
        {
            var panelSize = _root.layout;
            float x = panelPosition.x + Offset;
            float y = panelPosition.y + Offset;

            float w = _tooltip.layout.width;
            float h = _tooltip.layout.height;
            if (x + w > panelSize.width) x = panelPosition.x - w - Offset;
            if (y + h > panelSize.height) y = panelPosition.y - h - Offset;

            _tooltip.style.left = Mathf.Max(0, x);
            _tooltip.style.top = Mathf.Max(0, y);
        });
    }

    public void Hide()
    {
        if (_tooltip == null) return;
        _tooltip.style.display = DisplayStyle.None;
    }
}