using UnityEngine;
using System;

namespace FogClouds
{
    [CreateAssetMenu(menuName = "FogClouds/GameEvent")]
    public class GameEvent : ScriptableObject
    {
        [field: SerializeField] public string EventId { get; private set; }
        [field: SerializeField] public string DisplayName { get; private set; }
        [field: SerializeField] public string Description { get; private set; }
        [field: SerializeField] public string EffectId { get; private set; }
    }
}

