using UnityEngine;
using UnityEngine.EventSystems;

public class DraggableCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{

    
    
    [Header("Animation Settings")]

    public float opacity;
    public float hoverScale;

    [Header("Public Flags (Do Not Touch)")]

    public bool isDragging;
    
    private Canvas canvas;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector2 originalPosition;
    private Transform originalParent;
    private DeckManager deckManager;

    private ResourceManager resourceManager;

    private Vector3 originalScale;
    private int originalSiblingIndex;
    


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
        resourceManager = FindFirstObjectByType<ResourceManager>();

        originalScale = transform.localScale;
        

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
        canvasGroup.alpha = opacity;
        canvasGroup.blocksRaycasts = false;
        isDragging = true;
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
        isDragging = false;
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        transform.localScale = originalScale;



        if (!IsPointerOverHandContainer(eventData))
        {
            if (deckManager != null)
            {
                if (resourceManager.IsCardPlayable(gameObject))
                {
                    resourceManager.PlayCard(gameObject);
                }
            }
        }
        else
        {
            rectTransform.anchoredPosition = originalPosition;
            deckManager.RefreshHandLayout();
        }

        
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        transform.localScale = originalScale * hoverScale;
        originalSiblingIndex = transform.GetSiblingIndex();
        transform.SetAsLastSibling();
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isDragging)
        {
            transform.localScale = originalScale;

            transform.SetSiblingIndex(originalSiblingIndex);

            
        }

        deckManager.RefreshHandLayout();
    }
    

}
