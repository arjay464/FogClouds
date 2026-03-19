using UnityEngine;
using System;

namespace FogClouds
{
    [CreateAssetMenu(menuName = "FogClouds/GameEventDefinition")]
    public class GameEventDefinition : ScriptableObject
    {
        [field: SerializeField] public GameEvent[] EventPool { get; private set; }
    }
}