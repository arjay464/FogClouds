using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class ResourceManager : MonoBehaviour
{
    [Header("Resource Amounts [Thessa]")]
    public int startingHP;

    public int startingBloodVials;

    [Header("UI Elements")]

    public TextMeshProUGUI hpCounter;
    public TextMeshProUGUI bvCounter;
    public TextMeshProUGUI attackCounter;
    public TextMeshProUGUI drawCounter;
    public Dictionary<CardData.Cost, int> resourceDictionary;
    public Dictionary<CardData.Effect, int> outputDictionary;
    private DeckManager deckManager;
    private EffectManager effectManager;



    void Awake()
    {
        resourceDictionary = new Dictionary<CardData.Cost, int>
        {
            {CardData.Cost.hp, startingHP},
            {CardData.Cost.bloodVials, startingBloodVials}
        };

        outputDictionary = new Dictionary<CardData.Effect, int>
        {
            {CardData.Effect.attack, 0},
            {CardData.Effect.draw, 0},
            {CardData.Effect.shield, 0},
        };

        updateUICounters();
    }

    void Start()
    {
        deckManager = FindFirstObjectByType<DeckManager>();
        effectManager = FindFirstObjectByType<EffectManager>();
    }

    public bool IsCardPlayable(GameObject cardObject)
    {
        CardDisplay display = cardObject.GetComponent<CardDisplay>();

        CardData cardData = display.cardData;

        Dictionary<CardData.Cost, int> costDictionary = cardData.GetCostDictionary();

        foreach (var cost in costDictionary)
        {
            if (GetResource(cost.Key) < costDictionary[cost.Key])
            {
                return false;
            }
        }
        return true;
    }

    public void PlayCard(GameObject cardObject)
    {
        CardDisplay display = cardObject.GetComponent<CardDisplay>();

        CardData cardData = display.cardData;

        Dictionary<CardData.Cost, int> costDictionary = cardData.GetCostDictionary();

        Dictionary<CardData.Effect, int> effectDictionary = cardData.GetEffectDictionary();

        foreach (var cost in costDictionary)
        {
            resourceDictionary[cost.Key] -= cost.Value;
        }

        foreach (var effect in effectDictionary)
        {
            if (outputDictionary.ContainsKey(effect.Key))
            {
                outputDictionary[effect.Key] += effect.Value;
            }

            effectManager.ExecuteEffect(effect.Key, effect.Value);

        }


        deckManager.DiscardCard(cardObject);

        updateUICounters();
        
    }

    public int GetResource(CardData.Cost type)
    {
        return resourceDictionary.ContainsKey(type) ? resourceDictionary[type] : 0;
    }
    
    void updateUICounters()
    {
        hpCounter.text = $"HP: {resourceDictionary[CardData.Cost.hp].ToString()}";
        bvCounter.text = $"Blood Vials: {resourceDictionary[CardData.Cost.bloodVials].ToString()}";
        attackCounter.text = $"Attack: {outputDictionary[CardData.Effect.attack].ToString()}";
        drawCounter.text = $"Draw: {outputDictionary[CardData.Effect.draw].ToString()}";
    }
}
