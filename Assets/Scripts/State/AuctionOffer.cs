using System;
using System.Collections.Generic;

namespace FogClouds
{
    [Serializable]
    public class AuctionOffer
    {
        public List<string> CardIds;
        public int[] Player0Bids;
        public int[] Player1Bids;
        public bool Player0Submitted;
        public bool Player1Submitted;
        public List<string> CardDisplayNames;

        public AuctionOffer()
        {
            CardIds = new List<string>();
            Player0Bids = new int[] { -1, -1, -1 };
            Player1Bids = new int[] { -1, -1, -1 };
            CardDisplayNames = new List<string>();
            Player0Submitted = false;
            Player1Submitted = false;
        }

        public void Reset()
        {
            CardIds.Clear();
            Player0Bids = new int[] { -1, -1, -1 };
            Player1Bids = new int[] { -1, -1, -1 };
            CardDisplayNames.Clear();
            Player0Submitted = false;
            Player1Submitted = false;
        }
    }
}