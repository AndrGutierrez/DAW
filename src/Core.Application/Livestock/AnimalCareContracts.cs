using Core.Domain.Livestock;

namespace Core.Application.Livestock;

public enum ClinicalEventKind { Treatment, Vaccination, DiseaseCase, Deworming, Quarantine, Mortality }
public enum ReproductiveEventKind { Heat, Mating, Insemination, PregnancyCheck, Calving, Weaning, Abortion }

public sealed record ClinicalRequest(Guid SubmissionId, ClinicalEventKind Kind, DateOnly Date, string? Notes = null,
    Guid? ProductId = null, decimal? Dose = null, MedicationRoute Route = MedicationRoute.Other,
    DateOnly? EndDate = null, int? WithdrawalDays = null, DateOnly? NextDueDate = null,
    string? Severity = null, bool IsContagious = false, string? Reason = null, decimal? Cost = null);
public sealed record ReproductiveRequest(Guid SubmissionId, ReproductiveEventKind Kind, DateOnly Date, string? Notes = null,
    Guid? SireId = null, PregnancyResult? Result = null, string? Method = null, DateOnly? ExpectedCalvingDate = null,
    int? OffspringCount = null, int? StillbornCount = null, CalvingDifficulty Difficulty = CalvingDifficulty.Easy,
    Guid? OffspringId = null, decimal? WeightKg = null, string? Reason = null);
public sealed record CareSubmission<T>(Guid Id, bool Replayed, T Data);
public sealed record ClinicalRecord(Guid Id, DateTime CreatedAt, Guid? UserId, ClinicalRequest Data, DateOnly? WithdrawalEndDate, string? ProductName);
public sealed record ReproductiveRecord(Guid Id, DateTime CreatedAt, Guid? UserId, ReproductiveRequest Data);
public sealed record CarePageRequest(int Page = 1, int PageSize = 10);
public sealed record CarePage<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);
public sealed record WithdrawalStatus(bool Blocked, DateOnly? LastRestrictedDate, DateOnly? ReleaseDate, int Treatments);
public sealed record HealthChangeRecord(DateTime ChangedAt, HealthStatus PreviousStatus, HealthStatus NewStatus, string? Reason);
public sealed record ClinicalHistory(CarePage<ClinicalRecord> History, WithdrawalStatus Withdrawal, IReadOnlyList<HealthChangeRecord> StatusChanges);
public sealed record ReproductiveStatus(string State, DateOnly? Date, DateOnly? ExpectedCalvingDate);
public sealed record ReproductiveHistory(CarePage<ReproductiveRecord> History, ReproductiveStatus Status);
