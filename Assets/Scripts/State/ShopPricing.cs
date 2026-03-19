namespace FogClouds
{
    public static class ShopPricing
    {
        public static readonly (int min, int max) PowerCard = (4, 7);
        public static readonly (int min, int max) StrategyCard = (3, 6);
        public static readonly (int min, int max) ColorlessCard = (6, 10);
        public static readonly (int min, int max) Passive = (3, 8);
        public static readonly (int min, int max) HpRegenSmall = (2, 4);
        public static readonly (int min, int max) HpRegenLarge = (4, 8);
        public static readonly (int min, int max) SightSmall = (2, 3);
        public static readonly (int min, int max) SightLarge = (5, 7);
        public static readonly (int min, int max) PersistentResource = (3, 6);

        public static int Roll(System.Random rng, (int min, int max) range)
        {
            return rng.Next(range.min, range.max + 1);
        }
    }
}