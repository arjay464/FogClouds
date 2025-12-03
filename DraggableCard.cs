using UnityEngine;
using UnityEngine.EventSystems;

public class DraggableCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    private Canvas canvas;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector2 originalPosition;
    private Transform originalParent;
    private DeckManager deckManager;


    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    void Start()
    {
        canvas = GetComponentInParent<Canvas>();
        deckManager = FindFirstObjectByType<DeckManager>();

        if (canvas == null)
        {
            Debug.LogWarning("Canvas Parent not yet loaded.");
        }
        
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($"Clicked on {gameObject.name}");
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalPosition = rectTransform.anchoredPosition;
        originalParent = transform.parent;
        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (canvas != null)
        {
            rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
        }
        else
        {
            rectTransform.anchoredPosition += eventData.delta;
        }

    }
    
    private bool IsPointerOverHandContainer(PointerEventData eventData)
    {
        if (deckManager == null || deckManager.handContainer == null) return false;

        RectTransform handRect = deckManager.handContainer.GetComponent<RectTransform>();
        return RectTransformUtility.RectangleContainsScreenPoint(handRect, eventData.position, eventData.pressEventCamera);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        if (!IsPointerOverHandContainer(eventData))
        {
            if (deckManager != null)
            {
                deckManager.DiscardCard(gameObject);
            }
        }
        else
        {
            rectTransform.anchoredPosition = originalPosition;
        }
    }
    
    
    

}
