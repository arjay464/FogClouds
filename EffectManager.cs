using UnityEngine;
using System;
using System.Collections.Generic;
using Mirror;

public class EffectManager : NetworkBehaviour
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
    
    [Server]
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

    [Server]
    public void Draw(int value)
    {
        for (int i = 0; i < value; i++)
        {
            deckManager.CmdDrawCard();
        }
    }
    
    [Server]
    public void Attack(int value)
    {
        Debug.Log("Attacked!");
    }
 
}
