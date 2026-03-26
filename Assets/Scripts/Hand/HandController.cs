using UnityEngine;
using UnityEngine.UIElements;
using FogClouds;
using System.Collections.Generic;

public class HandController : MonoBehaviour
{
    private VisualElement _handArea;
    private VisualElement _playZone;
    private VisualElement _root;

    private List<CardView> _cardViews = new();
    private CardView _draggedCard = null;
    private VisualElement _dragGhost = null;
    private Vector2 _dragOffset;
    private bool _rebuildInProgress = false;

    // Fan settings
    private const float CardOverlap = 28f;
    private const float CardWidth = 80f;
    private const float CardHeight = 112f;

    void OnDisable()
    {
        if (ClientStateManager.Instance != null)
            ClientStateManager.Instance.OnStateUpdated -= Refresh;
    }

    public void Initialize(VisualElement root)
    {
        _root = root;
        _handArea = root.Q<VisualElement>("hand-area");
        _playZone = root.Q<VisualElement>("own-board");

        _root.RegisterCallback<PointerMoveEvent>(OnPointerMove);
        _root.RegisterCallback<PointerUpEvent>(OnPointerUp);
        _root.RegisterCallback<PointerCancelEvent>(OnPointerCancel);

        // Registered once here, not per card
        _root.RegisterCallback<PointerLeaveEvent>(evt =>
        {
            if (_draggedCard == null) return;
            _draggedCard.ReleasePointer(evt.pointerId);
            CompleteDrag(Vector2.zero, forceSnapBack: true);
        });

        if (ClientStateManager.Instance != null)
            ClientStateManager.Instance.OnStateUpdated += Refresh;
    }

    private void Refresh(ClientGameStateView view)
    {
        var hand = view.OwnState?.Hand;
        if (hand == null) return;

        bool changed = hand.Count != _cardViews.Count;
        if (!changed)
        {
            for (int i = 0; i < hand.Count; i++)
            {
                if (_cardViews[i].CardData.InstanceId != hand[i].InstanceId)
                {
                    changed = true;
                    break;
                }
            }
        }

        if (changed && !_rebuildInProgress)
            StartCoroutine(RebuildHand(view.OwnState.Hand));
        else
            ApplyFanLayout();
    }

    private System.Collections.IEnumerator RebuildHand(
        System.Collections.Generic.List<CardInstanceView> hand)
    {
        _rebuildInProgress = true;
        yield return null;
        while (_handArea?.panel == null) yield return null;

        // Snapshot the hand data before scheduling
        var snapshot = new List<CardInstanceView>(hand);

        // Schedule the mutation through UI Toolkit's own scheduler
        // This guarantees it runs between repaint passes, never during one
        _handArea.schedule.Execute(() =>
        {
            _handArea.Clear();
            _cardViews.Clear();

            foreach (var cardData in snapshot)
            {
                var cardView = new CardView(cardData);
                cardView.OnSelected += OnCardSelected;
                RegisterDragCallbacks(cardView);
                _cardViews.Add(cardView);
                _handArea.Add(cardView);
            }

            ApplyFanLayout();
            _rebuildInProgress = false;
        });
    }

    private void ApplyFanLayout()
    {
        int count = _cardViews.Count;
        if (count == 0) return;
        if (_handArea?.panel == null) return;

        for (int i = 0; i < count; i++)
        {
            var card = _cardViews[i];
            if (card == _draggedCard) continue;

            float xPos = i * (CardWidth - CardOverlap);

            card.style.width = CardWidth;
            card.style.height = CardHeight;
            card.style.position = Position.Absolute;
            card.style.left = xPos;
            card.style.bottom = 0;
            card.style.rotate = new StyleRotate(new Rotate(0));
            card.style.transformOrigin = StyleKeyword.Initial;
        }
    }

    private void RegisterDragCallbacks(CardView card)
    {
        // CardView.RegisterCallbacks — replace the right-click block:
        card.RegisterCallback<PointerDownEvent>(evt =>
        {
            if (evt.button != 0) return;
            if (_draggedCard != null) return;

            _draggedCard = card;
            _dragOffset = evt.localPosition;

            card.SetDragging(true);
            card.CapturePointer(evt.pointerId);

            // Ghost placeholder in hand
            _dragGhost = new VisualElement();
            _dragGhost.style.position = Position.Absolute;
            _dragGhost.style.width = 80;
            _dragGhost.style.height = 112;
            _dragGhost.style.opacity = 0.3f;
            _dragGhost.style.backgroundColor = new StyleColor(new Color(0.4f, 0.4f, 0.5f, 0.3f));
            _dragGhost.style.borderTopLeftRadius = 4;
            _dragGhost.style.borderTopRightRadius = 4;
            _dragGhost.style.borderBottomLeftRadius = 4;
            _dragGhost.style.borderBottomRightRadius = 4;
            _dragGhost.style.left = card.style.left;
            _dragGhost.style.bottom = card.style.bottom;
            _handArea.Add(_dragGhost);

            // Move card to root so it renders above everything
            _handArea.Remove(card);
            _root.Add(card);

            // Immediately position at pointer location in root space
            // so there's no snap on the first frame
            var rootPos = evt.position;
            card.style.position = Position.Absolute;
            card.style.left = rootPos.x - _dragOffset.x;
            card.style.top = rootPos.y - _dragOffset.y;
            card.style.bottom = StyleKeyword.Null;
            card.style.right = StyleKeyword.Null;

            ApplyFanLayout();
            evt.StopPropagation();
        });
    }

    private void OnPointerMove(PointerMoveEvent evt)
    {
        if (_draggedCard == null) return;

        var rootPos = evt.position;
        _draggedCard.style.position = Position.Absolute;
        _draggedCard.style.left = rootPos.x - _dragOffset.x;
        _draggedCard.style.top = rootPos.y - _dragOffset.y;

        bool overPlayZone = _playZone.worldBound.Contains(rootPos);
        _playZone.style.backgroundColor = overPlayZone
            ? new StyleColor(new Color(0.3f, 0.5f, 0.3f, 0.3f))
            : new StyleColor(new Color(0f, 0f, 0f, 0f));
    }

    private void OnPointerUp(PointerUpEvent evt)
    {
        if (_draggedCard == null) return;
        _draggedCard.ReleasePointer(evt.pointerId);
        CompleteDrag(evt.position);
    }

    private void OnPointerCancel(PointerCancelEvent evt)
    {
        if (_draggedCard == null) return;
        _draggedCard.ReleasePointer(evt.pointerId);
        CompleteDrag(Vector2.zero, forceSnapBack: true);
    }

    private void CompleteDrag(Vector2 pointerPosition, bool forceSnapBack = false)
    {
        bool overPlayZone = !forceSnapBack &&
                            _playZone.worldBound.Contains(pointerPosition);

        // Clean up ghost safely
        if (_dragGhost != null)
        {
            _dragGhost.parent?.Remove(_dragGhost);
            _dragGhost = null;
        }

        // Remove card from wherever it currently lives
        _draggedCard.parent?.Remove(_draggedCard);

        if (overPlayZone)
        {
            PlayCard(_draggedCard);
            _draggedCard.style.position = Position.Absolute;
            _draggedCard.style.left = StyleKeyword.Null;
            _draggedCard.style.top = StyleKeyword.Null;
            _handArea.Add(_draggedCard);
        }
        else
        {
            // Clear position overrides before snapping back to fan
            _draggedCard.style.position = Position.Absolute;
            _draggedCard.style.left = StyleKeyword.Null;
            _draggedCard.style.top = StyleKeyword.Null;
            _draggedCard.style.right = StyleKeyword.Null;
            _draggedCard.style.bottom = StyleKeyword.Null;
            _handArea.Add(_draggedCard);
        }

        _draggedCard.SetDragging(false);
        if (overPlayZone)
            _draggedCard.ResetState();  // played — wipe all transient state
        else
            _draggedCard.SetSelected(false);  // snapped back — keep upcast intent
        _draggedCard = null;

        _playZone.style.backgroundColor = new StyleColor(new Color(0f, 0f, 0f, 0f));

        ApplyFanLayout();
    }

    private void PlayCard(CardView card)
    {
        var data = card.CardData;
        var agent = PlayerNetworkAgent.LocalAgent;
        if (agent == null) return;

        switch (data.Type)
        {
            case CardType.Queueable:
                agent.CmdQueueCardById(data.InstanceId, card.IsUpcast);
                break;
            case CardType.Instant:
                agent.CmdPlayInstant(data.InstanceId);
                break;
            case CardType.Permanent:
                agent.CmdPlayPermanent(data.InstanceId);
                break;
        }
    }

    private void OnCardSelected(CardView selected)
    {
        foreach (var card in _cardViews)
        {
            if (card != selected)
                card.SetSelected(false);
        }
    }
}