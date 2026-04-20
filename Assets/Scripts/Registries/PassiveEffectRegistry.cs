using System.Collections.Generic;
using UnityEngine;
using FogClouds;

public class PassiveEffectRegistry : MonoBehaviour
{
    public static PassiveEffectRegistry Instance { get; private set; }

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
        _passives["season_of_harvest"] = new SeasonOfHarvestPassive();
        _passives["accelerator"] = new AcceleratorPassive();
        _passives["slight_of_hand"] = new SlightOfHandPassive();
        _passives["clutterstorm"] = new ClutterstormPassive();
        _passives["unbroken_chain"] = new UnbrokenChainPassive();
        _passives["natures_eye"] = new NaturesEyePassive();
        _passives["market_crash"] = new MarketCrashPassive();
        _passives["blessed_diary"] = new BlessedDiaryPassive();
        _passives["ancient_telescope"] = new AncientTelescopePassive();
        _passives["ceremonial_dagger"] = new CeremonialDaggerPassive();
        _passives["cha_cha_lifelong_companion"] = new ChaChaLifelongCompanionPassive();
        Debug.Log($"[PassiveEffectRegistry] Registered {_passives.Count} passives.");
    }

    public IPassiveEffect GetEffect(string passiveId)
    {
        if (_passives.TryGetValue(passiveId, out var effect))
            return effect;

        Debug.LogWarning($"[PassiveEffectRegistry] No effect found for id: {passiveId}");
        return null;
    }
}