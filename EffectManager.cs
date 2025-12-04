using UnityEngine;
using System;
using System.Collections.Generic;

public class EffectManager : MonoBehaviour
{

    private DeckManager deckManager;

    private Dictionary<CardData.Effect, Action<int>> effectMap;

    void Awake()
    {
        deckManager = FindFirstObjectByType<DeckManager>();

        effectMap = new Dictionary<CardData.Effect, Action<int>>
        {
            {CardData.Effect.draw, Draw},
            {CardData.Effect.attack, Attack}
        };

    }
    
    public void ExecuteEffect(CardData.Effect effectType, int value)
    {
        if (effectMap.ContainsKey(effectType))
        {
            effectMap[effectType](value);
        }
        else
        {
            Debug.LogWarning($"No implementation for effect {effectType}");
        }
    }

    public void Draw(int value)
    {
        for (int i = 0; i < value; i++)
        {
            deckManager.DrawCard();
        }
    }
    
    public void Attack(int value)
    {
        Debug.Log("Attacked!");
    }
    
    
}
