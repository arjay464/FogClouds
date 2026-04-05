using UnityEngine;
using UnityEngine.UIElements;
using FogClouds;
using System.Collections;
using System.Collections.Generic;

public class OpponentHandController : MonoBehaviour
{
    private VisualElement _opponentBoard;
    private List<OpponentCardView> _cardViews = new();
    private bool _rebuildInProgress = false;

    private const float CardWidth = 60f;
    private const float CardHeight = 84f;
    private const float CardOverlap = 20f;
    private const float FanSpread = 4f;
    private const float FanRaise = 8f;

    public void Initialize(VisualElement root)
    {
        _opponentBoard = root.Q<VisualElement>("opponent-board");

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
        var opp = view.OpponentState;
        if (opp == null) return;

        bool handContents = opp.Hand != null;
        bool handSize = opp.HandSize >= 0;

        if (!handSize && !handContents)
        {
            // No reveal — clear board
            if (_cardViews.Count > 0 && !_rebuildInProgress)
                StartCoroutine(RebuildHand(null, 0));
            return;
        }

        int count = handContents ? opp.Hand.Count : opp.HandSize;
        bool contentsChanged = handContents
            ? (opp.Hand.Count != _cardViews.Count)
            : (opp.HandSize != _cardViews.Count);

        if (contentsChanged && !_rebuildInProgress)
            StartCoroutine(RebuildHand(handContents ? opp.Hand : null, count));
        else
            ApplyFanLayout();
    }

    private IEnumerator RebuildHand(List<CardInstanceView> hand, int count)
    {
        _rebuildInProgress = true;
        yield return null;
#if UNITY_EDITOR
        yield return null;
#endif
        while (_opponentBoard?.panel == null) yield return null;

        _opponentBoard.schedule.Execute(() =>
        {
            _opponentBoard.Clear();
            _cardViews.Clear();

            if (hand != null)
            {
                // HandContents revealed — show face-up cards
                foreach (var cardData in hand)
                {
                    var card = new OpponentCardView(cardData);
                    _cardViews.Add(card);
                    _opponentBoard.Add(card);
                }
            }
            else
            {
                // HandSize only — show face-down cards
                for (int i = 0; i < count; i++)
                {
                    var card = new OpponentCardView();
                    _cardViews.Add(card);
                    _opponentBoard.Add(card);
                }
            }

            ApplyFanLayout();
            _rebuildInProgress = false;
        });
    }

    private void ApplyFanLayout()
    {
        int count = _cardViews.Count;
        if (count == 0) return;
        if (_opponentBoard?.panel == null) return;

        float centerIndex = (count - 1) / 2f;

        for (int i = 0; i < count; i++)
        {
            var card = _cardViews[i];
            float offset = i - centerIndex;
            float rotation = offset * FanSpread;
            float raise = centerIndex == 0 ? FanRaise :
                          FanRaise - Mathf.Abs(offset) * (FanRaise / centerIndex);
            float xPos = i * (CardWidth - CardOverlap);

            card.style.width = CardWidth;
            card.style.height = CardHeight;
            card.style.position = Position.Absolute;
            card.style.left = xPos;
            card.style.top = raise;
            card.style.rotate = new StyleRotate(new Rotate(rotation));
            card.style.transformOrigin = new StyleTransformOrigin(
                new TransformOrigin(Length.Percent(50), Length.Percent(110)));
        }
    }
}