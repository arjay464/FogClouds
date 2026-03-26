namespace FogClouds
{
    public static class ShopPricing
    {
        public static readonly (int min, int max) PowerCard = (48, 48);
        public static readonly (int min, int max) StrategyCard = (48, 48);
        public static readonly (int min, int max) ColorlessCard = (55, 55);
        public static readonly (int min, int max) Passive = (60, 60);
        public static readonly (int min, int max) HpRegenSmall = (25, 25);
        public static readonly (int min, int max) HpRegenLarge = (40, 40);
        public static readonly (int min, int max) SightSmall = (25, 25);
        public static readonly (int min, int max) SightLarge = (40, 40);
        public static readonly (int min, int max) PerTurnResource = (60, 60);

        public static int Roll(System.Random rng, (int min, int max) range)
        {
            return rng.Next(range.min, range.max + 1);
        }
    }
}