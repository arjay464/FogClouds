using UnityEngine;
using System;
using System.Collections.Generic;

namespace FogClouds
{
    [CreateAssetMenu(menuName = "FogClouds/InsightTreeDefinition")]
    public class InsightTreeDefinition : ScriptableObject
    {
        [field: SerializeField] public InsightTreeNode[] AllNodes { get; private set; }
    }

    [Serializable]
    public class InsightTreeState
    {
        //Set of node IDs that have been unlocked.
        public List<string> UnlockedNodes;
        public int SightBanked;

        public InsightTreeState()
        {
            UnlockedNodes = new List<string>();
            SightBanked = 0;
        }


        public bool IsUnlocked(string nodeId) => UnlockedNodes.Contains(nodeId);

        public void Unlock(string nodeId)
        {
            if (!IsUnlocked(nodeId))
                UnlockedNodes.Add(nodeId);
        }

        public InsightTreeState Clone()
        {
            return new InsightTreeState
            {
                UnlockedNodes = new List<string>(this.UnlockedNodes),
                SightBanked = this.SightBanked
            };
        }
    }

}