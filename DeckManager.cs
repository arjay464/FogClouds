using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;


public class DeckManager : MonoBehaviour
{
    [Header("Card Prefab")]
    public GameObject cardPrefab;

    [Header("UI Containers")]
    public Transform drawPileTransform;
    public Transform discardPileTransform;
    public Transform handContainer;

    [Header("Card Data")]
    public List<CardData> playerDeck = new List<CardData>();

    [Header("Input")]
    public InputActionAsset drawDiscardTest;

    [Header("Card Fan Settings")]
    public float cardSpacing; 
    public float arcSeverity;
    public float maxRotation; 
    private InputAction drawAction;
    private List<CardData> drawPile = new List<CardData>();
    private List<CardData> discardPile = new List<CardData>();
    private List<GameObject> cardsInHand = new List<GameObject>();


    void Awake()
    {
        var gameplayMap = drawDiscardTest.FindActionMap("drawDiscardTest");
        drawAction = gameplayMap.FindAction("Draw");
    }

    void OnEnable()
    {
        drawAction.Enable();
        drawAction.performed += OnDrawPerformed;
    }
    
    void OnDisable()
    {
        drawAction.performed -= OnDrawPerformed;
        drawAction.Disable();
    }
    void Start()
    {
        InitializeDeck();
    }

    private void OnDrawPerformed(InputAction.CallbackContext context)
    {
        DrawCard();
    }


    void InitializeDeck()
    {
        drawPile.AddRange(playerDeck);
        ShuffleDeck();
    }

    public void DrawCard()
    {
        if (drawPile.Count == 0)
        {
            if (discardPile.Count == 0)
            {
                Debug.Log("No cards left to draw.");
                return;
            }

            Debug.Log("Shuffling Deck.");
            drawPile.AddRange(discardPile);
            ShuffleDeck();
            discardPile.Clear();

        }

        CardData drawnCard = drawPile[0];
        drawPile.RemoveAt(0);

        GameObject cardObject = Instantiate(cardPrefab, handContainer);
        CardDisplay display = cardObject.GetComponent<CardDisplay>();
        display.SetCardData(drawnCard);

        cardsInHand.Add(cardObject);

        RefreshHandLayout();

        Debug.Log($"Drew {drawnCard.cardName}. {drawPile.Count} left in draw pile.");

    }

    public void DiscardCard(GameObject cardObject)
    {
        CardDisplay display = cardObject.GetComponent<CardDisplay>();

        if (display != null && display.cardData != null)
        {
            discardPile.Add(display.cardData);
            Debug.Log($"Discarded {display.cardData.cardName}");
        }

        cardsInHand.Remove(cardObject);

        Destroy(cardObject);

        RefreshHandLayout();
    }

    void ShuffleDeck()
    {
        //Fisher-Yates shuffle is a w algorithm -Arjay

        for (int i = drawPile.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            CardData temp = drawPile[i];
            drawPile[i] = drawPile[randomIndex];
            drawPile[randomIndex] = temp;
        }
    }

    void RefreshHandLayout()
    {
        if (cardsInHand.Count <= 1)
        {
            return;
        } 

        for (int i = 0; i < cardsInHand.Count; i++)
        {
            float normalizedPosition = i / (float)(cardsInHand.Count - 1) * 2f - 1f;
            float totalWidth = (cardsInHand.Count - 1) * cardSpacing;
            float xPosition = -totalWidth / 2f + i * cardSpacing;
            float yPosition = Mathf.Abs(normalizedPosition) * arcSeverity;
            float rotation = normalizedPosition * maxRotation * -1f;

            RectTransform rT = cardsInHand[i].GetComponent<RectTransform>();
            rT.anchoredPosition = new Vector2(xPosition, yPosition);
            rT.localRotation = Quaternion.Euler(0, 0, rotation);
        }
        

    }
    
    
}
