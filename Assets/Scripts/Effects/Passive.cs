using System;
using UnityEngine;

namespace FogClouds
{
    [Serializable]
    public class Passive
    {
        public string PassiveId;
        public string DisplayName;
        public int StackCount;
        public string TargetPermanentId;
        public bool IsExhausted;

        public Passive() { }

        public Passive Clone()
        {
            return new Passive
            {
                PassiveId = this.PassiveId,
                DisplayName = this.DisplayName,
                StackCount = this.StackCount,
                TargetPermanentId = this.TargetPermanentId,
                IsExhausted = this.IsExhausted
            };
        }
    }
}
