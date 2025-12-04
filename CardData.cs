using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class EffectEntry
{
    public CardData.Effect effect;
    public int value;
}

[System.Serializable]
public class CostEntry
{
    public CardData.Cost cost;
    public int value;
}

[CreateAssetMenu(fileName = "CardData", menuName = "Scriptable Objects/CardData")]
public class CardData : ScriptableObject
{
    public enum CardPool
    {
        thessandria,
        elarion,
        wildfire
    }

    public enum Effect
    {
        //general
        attack,
        shield,
        turnPriorityIncrease,
        draw,
        followingAttacksLifesteal,
        lifesteal
    }

    public enum Cost
    {
        hp,
        bloodVials,
        mistforce,
        gold,

    }


    public string cardName;

    public string description;
    public List<EffectEntry> effectList;
    public List<CostEntry> costList;

    public int priority;

    public bool usesStack;

    public CardPool cardPool;

    public Sprite cardArt;


    public Dictionary<Effect, int> GetEffectDictionary()
    {
        Dictionary<Effect, int> dict = new Dictionary<Effect, int>();
        foreach (var entry in effectList)
        {
            dict[entry.effect] = entry.value;
        }
        return dict;
    }
    
    public Dictionary<Cost, int> GetCostDictionary()
    {
        Dictionary<Cost, int> dict = new Dictionary<Cost, int>();
        foreach (var entry in costList)
        {
            dict[entry.cost] = entry.value;
        }
        return dict;
    }
}
