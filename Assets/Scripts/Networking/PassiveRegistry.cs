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
        _passives["pact_of_the_devil"] = new PactOfTheDevilPassive();
        _passives["blessing_of_valor"] = new BlessingOfValorPassive();
        _passives["blessing_of_clarity"] = new BlessingOfClarityPassive();
        _passives["blessing_of_grace"] = new BlessingOfGracePassive();
        _passives["blessing_of_fortitude"] = new BlessingOfFortitudePassive();
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