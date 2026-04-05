using System;
using UnityEngine;

namespace FogClouds
{
    [CreateAssetMenu(menuName = "FogClouds/PassiveDefinition")]
    public class PassiveDefinition : ScriptableObject
    {
        public string PassiveId;
        public string DisplayName;
        public string Description;
        public int Cost; // Silver cost at Shop
    }
}

