using System.Collections.Generic;
using UnityEngine;
using FogClouds;

public class CharacterLibrary : MonoBehaviour
{
    public static CharacterLibrary Instance { get; private set; }

    private Dictionary<string, CharacterData> _characters = new();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        LoadAll();
    }

    private void LoadAll()
    {
        var all = Resources.LoadAll<CharacterData>("Characters");
        foreach (var data in all)
            _characters[data.CharacterId] = data;

        Debug.Log($"[CharacterLibrary] Loaded {_characters.Count} characters.");
    }

    public CharacterData Get(string characterId)
    {
        if (_characters.TryGetValue(characterId, out var data))
            return data;

        Debug.LogWarning($"[CharacterLibrary] No character found for id: {characterId}");
        return null;
    }
}