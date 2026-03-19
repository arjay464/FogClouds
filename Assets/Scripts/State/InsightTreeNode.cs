using System;
using UnityEngine;

namespace FogClouds
{
    [CreateAssetMenu(menuName = "FogClouds/InsightTreeNode")]
    public class InsightTreeNode : ScriptableObject
    {
        [field: SerializeField] public string NodeId { get; private set; }
        [field: SerializeField] public string DisplayName { get; private set; }
        [field: SerializeField] public string Description { get; private set; }
        [field: SerializeField] public int Cost { get; private set; }
        [field: SerializeField] public string FlagName { get; private set; } // matches FogRevealState field name
        [field: SerializeField] public InsightTreeNode[] Prerequisites { get; private set; }
    }
}