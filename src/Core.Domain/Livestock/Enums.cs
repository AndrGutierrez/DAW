namespace Core.Domain.Livestock;

public enum Sex
{
    Male,
    Female
}

public enum AnimalStatus
{
    Active,
    Sold,
    Dead,
    Transferred,
    Lost
}

public enum AnimalOrigin
{
    Born,
    Purchased
}

public enum ProductivePurpose
{
    Meat,
    Milk,
    Wool,
    Eggs,
    DualPurpose,
    Work
}

public enum HealthStatus
{
    Healthy,
    UnderObservation,
    InTreatment,
    Quarantine,
    Critical
}

public enum MedicationRoute
{
    Oral,
    Subcutaneous,
    Intramuscular,
    Intravenous,
    Topical,
    Other
}

public enum ReproductionMethod
{
    Natural,
    ArtificialInsemination
}

public enum PregnancyResult
{
    Positive,
    Negative,
    Uncertain
}

public enum CalvingDifficulty
{
    Easy,
    Assisted,
    Difficult,
    Cesarean
}

public enum ProductCategory
{
    Medicine,
    Vaccine,
    Feed,
    Supply,
    Semen,
    Other
}

public enum MeasurementUnit
{
    Kilogram,
    Liter,
    Unit,
    Dose,
    Bag,
    Micron,
    Percent,
    Hectare
}

public enum StockMovementType
{
    In,
    Out,
    Adjustment
}

public enum MilkShift
{
    Morning,
    Evening
}

public enum TransactionType
{
    Income,
    Expense
}

public enum TransactionCategory
{
    SaleAnimal,
    SaleMilk,
    SaleWool,
    SaleEggs,
    PurchaseFeed,
    PurchaseMedicine,
    PurchaseAnimal,
    Salary,
    Utilities,
    Other
}

public enum TaskType
{
    Vaccination,
    Weighing,
    Movement,
    Reproduction,
    Feeding,
    HealthCheck,
    Maintenance,
    Other
}

public enum TaskStatus
{
    Pending,
    InProgress,
    Completed,
    Cancelled
}

public enum TaskPriority
{
    Low,
    Medium,
    High,
    Urgent
}

public enum AlertType
{
    UpcomingVaccination,
    PregnancyCheckDue,
    LowStock,
    LowWeightGain,
    HighMortality,
    ExpiringProduct,
    Other
}

public enum AlertSeverity
{
    Info,
    Warning,
    Critical
}
