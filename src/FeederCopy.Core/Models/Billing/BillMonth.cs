namespace FeederCopy.Core.Models.Billing
{
    public readonly record struct BillMonth(int Year, int Month) : IComparable<BillMonth>
    {
        private static readonly string[] Names =
            ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];

        public int Index => Year * 12 + (Month - 1);

        public string Key => $"{Year:D4}{Month:D2}";

        public string ShortLabel => IsValid ? $"{Names[Month - 1]} '{Year % 100:D2}" : string.Empty;

        private bool IsValid => Month is >= 1 and <= 12;

        public static BillMonth FromIndex(int index) => new(index / 12, index % 12 + 1);

        public BillMonth AddMonths(int months) => FromIndex(Index + months);

        public static bool TryParse(string? text, out BillMonth month)
        {
            month = default;
            string? trimmed = text?.Trim();
            if (trimmed is null || trimmed.Length != 6 || !trimmed.All(char.IsAsciiDigit))
                return false;
            int year = int.Parse(trimmed[..4]);
            int monthNumber = int.Parse(trimmed[4..]);
            if (monthNumber is < 1 or > 12 || year < 1900)
                return false;
            month = new BillMonth(year, monthNumber);
            return true;
        }

        public static bool TryParseBillingText(string? text, out BillMonth month)
        {
            month = default;
            string digits = text?.Trim() ?? string.Empty;
            if (digits.Length == 0 || !digits.All(char.IsAsciiDigit))
                return false;
            if (digits.Length is 3 or 5)
                digits = "0" + digits;

            int year;
            int monthNumber;
            if (digits.Length == 4)
            {
                monthNumber = int.Parse(digits[..2]);
                year = 2000 + int.Parse(digits[2..]);
            }
            else if (digits.Length == 6 && int.Parse(digits[..4]) >= 1900)
            {
                year = int.Parse(digits[..4]);
                monthNumber = int.Parse(digits[4..]);
            }
            else if (digits.Length == 6)
            {
                monthNumber = int.Parse(digits[..2]);
                year = int.Parse(digits[2..]);
            }
            else
            {
                return false;
            }

            if (monthNumber is < 1 or > 12 || year < 1900)
                return false;
            month = new BillMonth(year, monthNumber);
            return true;
        }

        public static BillMonth Parse(string text) =>
            TryParse(text, out var month) ? month : throw new FormatException($"Invalid bill month '{text}'. Expected YYYYMM.");

        public int CompareTo(BillMonth other) => Index.CompareTo(other.Index);

        public static bool operator <(BillMonth left, BillMonth right) => left.Index < right.Index;
        public static bool operator >(BillMonth left, BillMonth right) => left.Index > right.Index;
        public static bool operator <=(BillMonth left, BillMonth right) => left.Index <= right.Index;
        public static bool operator >=(BillMonth left, BillMonth right) => left.Index >= right.Index;

        public override string ToString() => IsValid ? $"{Names[Month - 1]} {Year}" : string.Empty;
    }
}

