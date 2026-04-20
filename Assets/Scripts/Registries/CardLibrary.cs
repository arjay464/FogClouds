using System.Collections.Generic;
using UnityEngine;
using FogClouds;

public class CardLibrary : MonoBehaviour
{
    public static CardLibrary Instance { get; private set; }

    private Dictionary<string, CardDefinition> _definitions = new();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        LoadAll();
    }

    private void LoadAll()
    {
        var all = Resources.LoadAll<CardDefinition>("Cards");
        foreach (var def in all)
            _definitions[def.CardId] = def;

        Debug.Log($"[CardLibrary] Loaded {_definitions.Count} card definitions.");
    }

    public CardDefinition Get(string cardId)
    {
        if (_definitions.TryGetValue(cardId, out var def))
            return def;

        Debug.LogWarning($"[CardLibrary] No definition found for card id: {cardId}");
        return null;
    }
}