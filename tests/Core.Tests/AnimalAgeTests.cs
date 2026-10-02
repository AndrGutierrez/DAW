using Core.Domain.Livestock;

namespace Core.Tests;

public sealed class AnimalAgeTests
{
    [Theory]
    [InlineData("2024-01-10", "2026-01-10", 24)]
    [InlineData("2024-01-10", "2026-01-09", 23)]
    [InlineData("2024-01-10", "2024-01-10", 0)]
    public void InMonthsUsesCalendarMonths(string birth, string asOf, int expected)
    {
        var result = AnimalAge.InMonths(DateOnly.Parse(birth), DateOnly.Parse(asOf));

        Assert.Equal(expected, result);
    }

    [Fact]
    public void InMonthsNeverReturnsNegative()
    {
        var result = AnimalAge.InMonths(new DateOnly(2030, 1, 1), new DateOnly(2026, 1, 1));

        Assert.Equal(0, result);
    }

    [Theory]
    [InlineData("2020-06-15", "2026-06-15", 6)]
    [InlineData("2020-06-15", "2026-06-14", 5)]
    public void InYearsDerivesFromMonths(string birth, string asOf, int expected)
    {
        var result = AnimalAge.InYears(DateOnly.Parse(birth), DateOnly.Parse(asOf));

        Assert.Equal(expected, result);
    }
}
