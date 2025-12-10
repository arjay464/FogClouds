using UnityEngine;
using System.Collections.Generic;
using TMPro;
using Mirror;

public class ResourceManager : NetworkBehaviour
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

    }

    void Start()
    {
        if(isLocalPlayer){

            hpCounter = GameObject.Find("HP").GetComponent<TextMeshProUGUI>();
            bvCounter = GameObject.Find("Blood Vials").GetComponent<TextMeshProUGUI>();
            attackCounter = GameObject.Find("Attack").GetComponent<TextMeshProUGUI>();
            drawCounter = GameObject.Find("Draw").GetComponent<TextMeshProUGUI>();

            updateUICounters();
        }
    }

    public int GetResource(CardData.Cost type)
    {
        return resourceDictionary.ContainsKey(type) ? resourceDictionary[type] : 0;
    }

    public void updateResources(Dictionary<CardData.Cost, int> costDictionary){
        
        foreach (var cost in costDictionary)
        {
            resourceDictionary[cost.Key] -= cost.Value;
        }

        updateUICounters();
    }

    public void updateOutputs(Dictionary<CardData.Effect, int> effectDictionary){
        
        foreach (var effect in effectDictionary)
        {
            if (outputDictionary.ContainsKey(effect.Key))
            {
                outputDictionary[effect.Key] += effect.Value;
            }

        }

        updateUICounters();
    }
    
    void updateUICounters()
    {
        hpCounter.text = $"HP: {resourceDictionary[CardData.Cost.hp].ToString()}";
        bvCounter.text = $"Blood Vials: {resourceDictionary[CardData.Cost.bloodVials].ToString()}";
        attackCounter.text = $"Attack: {outputDictionary[CardData.Effect.attack].ToString()}";
        drawCounter.text = $"Draw: {outputDictionary[CardData.Effect.draw].ToString()}";
    }
}
