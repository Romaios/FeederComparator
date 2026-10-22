namespace FeederCopy.Core.Models.Feeders
{
    public static class RowCounts
    {
        public static int[] For(IReadOnlyList<bool> active)
        {
            var counts = new int[active.Count];
            int seen = 0;
            for (int i = 0; i < active.Count; i++)
                counts[i] = active[i] ? ++seen : seen + 1;
            return counts;
        }
    }
}
