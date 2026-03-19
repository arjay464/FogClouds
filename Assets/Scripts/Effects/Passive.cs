using System;
using UnityEngine;

namespace FogClouds
{
    [Serializable]
    public class Passive
    {
        public string PassiveId;
        public string DisplayName;
        public int StackCount; // some passives stack

        public Passive() { }

        public Passive Clone()
        {
            return new Passive
            {
                PassiveId = this.PassiveId,
                DisplayName = this.DisplayName,
                StackCount = this.StackCount
            };
        }
    }
}