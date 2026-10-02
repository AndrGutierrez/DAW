namespace Core.Domain.Livestock;

public static class AnimalAge
{
    public static int InMonths(DateOnly birthDate, DateOnly asOf)
    {
        var months = ((asOf.Year - birthDate.Year) * 12) + asOf.Month - birthDate.Month;

        if (asOf.Day < birthDate.Day)
        {
            months--;
        }

        return Math.Max(months, 0);
    }

    public static int InYears(DateOnly birthDate, DateOnly asOf) => InMonths(birthDate, asOf) / 12;
}
