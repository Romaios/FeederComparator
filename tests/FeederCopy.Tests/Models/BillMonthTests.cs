using FeederCopy.Core.Models.Billing;

namespace FeederCopy.Tests.Models
{
    public class BillMonthTests
    {
        [Theory]
        [InlineData("202601", 2026, 1)]
        [InlineData(" 202612 ", 2026, 12)]
        public void TryParse_accepts_yyyymm(string text, int year, int month)
        {
            Assert.True(BillMonth.TryParse(text, out var m));
            Assert.Equal(new BillMonth(year, month), m);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("2026")]
        [InlineData("202613")]
        [InlineData("202600")]
        [InlineData("abcdef")]
        [InlineData("2026-01")]
        public void TryParse_rejects_invalid(string? text) => Assert.False(BillMonth.TryParse(text, out _));

        [Theory]
        [InlineData("202607", 2026, 7)]
        [InlineData("072026", 2026, 7)]
        [InlineData("0825", 2025, 8)]
        [InlineData(" 1225 ", 2025, 12)]
        [InlineData("825", 2025, 8)]
        [InlineData("72026", 2026, 7)]
        public void Billing_text_accepts_year_month_and_month_year(string text, int year, int month)
        {
            Assert.True(BillMonth.TryParseBillingText(text, out var m));
            Assert.Equal(new BillMonth(year, month), m);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("1325")]
        [InlineData("0025")]
        [InlineData("202613")]
        [InlineData("132026")]
        [InlineData("08-25")]
        [InlineData("2025080")]
        public void Billing_text_rejects_invalid(string? text) => Assert.False(BillMonth.TryParseBillingText(text, out _));

        [Fact]
        public void AddMonths_crosses_year_boundaries()
        {
            Assert.Equal(new BillMonth(2025, 12), new BillMonth(2026, 1).AddMonths(-1));
            Assert.Equal(new BillMonth(2027, 2), new BillMonth(2026, 7).AddMonths(7));
        }

        [Fact]
        public void Key_and_labels()
        {
            var m = new BillMonth(2026, 7);
            Assert.Equal("202607", m.Key);
            Assert.Equal("Jul 2026", m.ToString());
            Assert.Equal("Jul '26", m.ShortLabel);
            Assert.Equal(string.Empty, default(BillMonth).ToString());
        }

        [Fact]
        public void Comparison_orders_chronologically()
        {
            Assert.True(new BillMonth(2025, 12) < new BillMonth(2026, 1));
            Assert.True(new BillMonth(2026, 3) >= new BillMonth(2026, 3));
            Assert.Equal(new BillMonth(2026, 1), new[] { new BillMonth(2026, 5), new BillMonth(2026, 1) }.Min());
        }
    }
}


