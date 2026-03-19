using System.Collections.Generic;
using UnityEngine;
using FogClouds;

public class PassiveRegistry : MonoBehaviour
{
    public static PassiveRegistry Instance { get; private set; }

    private Dictionary<string, IPassiveEffect> _passives = new();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        RegisterPassives();
    }

    private void RegisterPassives()
    {
        // Phase 5: register passives here
        // _passives["sigil_amplifier"] = new SigilAmplifierPassive();

        Debug.Log($"[PassiveRegistry] Registered {_passives.Count} passives.");
    }

    public IPassiveEffect GetEffect(string passiveId)
    {
        if (_passives.TryGetValue(passiveId, out var effect))
            return effect;

        Debug.LogWarning($"[PassiveRegistry] No effect found for id: {passiveId}");
        return null;
    }
}