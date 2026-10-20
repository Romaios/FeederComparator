namespace FeederCopy.Core.Models
{
    public static class Text
    {
        public static string Count(int n, string one, string many) => $"{n:N0} {(n == 1 ? one : many)}";
    }
}
