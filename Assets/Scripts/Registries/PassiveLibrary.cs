using System.Collections.Generic;
using UnityEngine;
using FogClouds;

public class PassiveLibrary : MonoBehaviour
{
    public static PassiveLibrary Instance { get; private set; }

    private Dictionary<string, PassiveDefinition> _definitions = new();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        LoadAll();
    }

    private void LoadAll()
    {
        var all = Resources.LoadAll<PassiveDefinition>("Passives");
        foreach (var def in all)
            _definitions[def.PassiveId] = def;

        Debug.Log($"[PassiveLibrary] Loaded {_definitions.Count} passive definitions.");
    }

    public PassiveDefinition Get(string passiveId)
    {
        if (_definitions.TryGetValue(passiveId, out var def))
            return def;

        Debug.LogWarning($"[PassiveLibrary] No definition found for passive id: {passiveId}");
        return null;
    }
}