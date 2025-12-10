using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Mirror;


public class DeckManager : NetworkBehaviour
{
    [Header("Card Prefab")]
    public GameObject cardPrefab;

    [Header("UI Containers (Set at runtime)")]

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

    //Server
    
    private List<CardInstance> drawPile = new List<CardInstance>();
    private List<CardInstance> discardPile = new List<CardInstance>();
    private List<CardInstance> serverCardsInHand = new List<CardInstance>();

    //Client
    private List<GameObject> cardsInHand = new List<GameObject>();

    //References

    private ResourceManager resourceManager;
    private EffectManager effectManager;

    void Awake()
    {
        if(isLocalPlayer){

            var gameplayMap = drawDiscardTest.FindActionMap("drawDiscardTest");
            drawAction = gameplayMap.FindAction("Draw");
            resourceManager = FindFirstObjectByType<ResourceManager>();
            effectManager = FindFirstObjectByType<EffectManager>();

        }
    }

    void OnEnable()
    {
        if(isLocalPlayer){

            drawAction.Enable();
            drawAction.performed += OnDrawPerformed;

        }
        
    }

    void OnDisable()
    {
        if(isLocalPlayer){

            drawAction.performed -= OnDrawPerformed;
            drawAction.Disable();

        }
    }

    void Start()
    {
        if(isLocalPlayer){
            
            CmdInitializeDeck();
            handContainer = GameObject.Find("HandContainer").transform;
            drawPileTransform = GameObject.Find("DrawPile").transform;
            discardPileTransform = GameObject.Find("DiscardPile").transform;

        }    
    }

    private void OnDrawPerformed(InputAction.CallbackContext context)
    {
        if(!isLocalPlayer) return;
            
        CmdDrawCard();  
    }

    [Command]
    void CmdInitializeDeck()
    {
        foreach(CardData cardData in playerDeck){
            
            CardInstance instance = new CardInstance(cardData);
            drawPile.Add(instance);

        }
        
        ShuffleDeck();
    }

    [Command]
    public void CmdDrawCard()
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

        CardInstance drawnCard = drawPile[0];
        drawPile.RemoveAt(0);

        TargetCardDraw(connectionToClient, drawnCard.cardData, drawnCard.instanceID);
    }

    [TargetRpc]
    void TargetCardDraw(NetworkConnection target, CardData cardData, int instanceID)
    {
        GameObject cardObject = Instantiate(cardPrefab, handContainer);
        CardDisplay display = cardObject.GetComponent<CardDisplay>();
        display.SetCardData(cardData, instanceID);
        cardsInHand.Add(cardObject);
        RefreshHandLayout();
        Debug.Log($"Drew {cardData.cardName} (id = {instanceID}). {drawPile.Count} left in draw pile.");
    }

    
    public void OnCardDiscarded(GameObject cardObject){
        
        if(!isLocalPlayer) return;

        CardDisplay display = cardObject.GetComponent<CardDisplay>();

        CmdDiscardCard(display.instanceID);

    }
    
    [Command]
    public void CmdDiscardCard(int instanceID)
    {
        
        CardInstance instance = serverCardsInHand.Find(c => c.instanceID == instanceID);

        if(instance == null) return;

        serverCardsInHand.Remove(instance);

        discardPile.Add(instance);

        TargetDiscardCard(connectionToClient, instance.instanceID);
    }

    [TargetRpc]
    void TargetDiscardCard(NetworkConnection target, int instanceID)
    {
        GameObject cardObject = cardsInHand.Find(obj => obj.GetComponent<CardDisplay>().instanceID == instanceID);

        if(cardObject == null) return;

        cardsInHand.Remove(cardObject);
        Destroy(cardObject);

        RefreshHandLayout();
    }

    [Server]
    void ShuffleDeck()
    {
        //Fisher-Yates shuffle is a w algorithm -Arjay

        for (int i = drawPile.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            CardInstance temp = drawPile[i];
            drawPile[i] = drawPile[randomIndex];
            drawPile[randomIndex] = temp;
        }
    }

    [Client]
    public void RefreshHandLayout()
    {
        if (cardsInHand.Count == 0)
        {
            return;
        }

        for (int i = 0; i < cardsInHand.Count; i++)
        {
            DraggableCard dC = cardsInHand[i].GetComponent<DraggableCard>();

            if (!dC.isDragging)
            {

                cardsInHand[i].transform.SetSiblingIndex(i);
                float normalizedPosition = 0;

                if (cardsInHand.Count > 1)
                {
                    normalizedPosition = i / (float)(cardsInHand.Count - 1) * 2f - 1f;
                }

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

    public void OnTurnEnd()
    {       
        if(!isLocalPlayer) return;
        
        while(cardsInHand.Count > 0)
        {
            OnCardDiscarded(cardsInHand[0]);
        }
    }

    public void OnCardPlayed(GameObject cardObject){

        if(!isLocalPlayer) return;

        CardInstance instance = cardObject.GetComponent<CardInstance>();

        CmdPlayCard(instance.instanceID); 
    }

    [Command]
    public void CmdPlayCard(int instanceID)
    {
        CardInstance instance = serverCardsInHand.Find(c => c.instanceID == instanceID);

        if(instance == null) return;

        Dictionary<CardData.Cost, int> costDictionary = instance.cardData.GetCostDictionary();

        Dictionary<CardData.Effect, int> effectDictionary = instance.cardData.GetEffectDictionary();

        if(!IsCardPlayable(instance, costDictionary)) return;


        TargetPlayCard(connectionToClient, instanceID);
        
    }

    [TargetRpc]
    public void TargetPlayCard(NetworkConnection target, int instanceID){

        GameObject cardObject = cardsInHand.Find(obj => obj.GetComponent<CardInstance>().instanceID == instanceID);

        CardInstance instance = cardObject.GetComponent<CardInstance>();

        Dictionary<CardData.Effect, int> effectDictionary = instance.cardData.GetEffectDictionary();

        resourceManager.updateOutputs(effectDictionary); 

        OnCardDiscarded(cardObject);

    }


    [Server]
    public bool IsCardPlayable(CardInstance instance, Dictionary<CardData.Cost, int> costDictionary)
    {
        CardData cardData = instance.cardData;

        foreach (var cost in costDictionary)
        {
            if (resourceManager.GetResource(cost.Key) < costDictionary[cost.Key]) return false;
        }
        return true;
    }
    
}
