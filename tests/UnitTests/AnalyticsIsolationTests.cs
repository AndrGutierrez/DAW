using Core.Application.Operations;
using Core.Application.Security;
using Core.Application.Livestock;
using FluentValidation;
using Moq;
namespace UnitTests;
public sealed class AnalyticsIsolationTests
{
    private readonly Mock<IOperationsReader> reader = new(MockBehavior.Strict);
    private readonly Mock<IFarmAccess> farms = new(MockBehavior.Strict);
    private readonly Guid farm = Guid.NewGuid();
    private readonly DateOnly day = DateOnly.FromDateTime(DateTime.UtcNow);
    [Fact]
    public async Task AnalyticsReadsOnlyAuthorizedFarmsAndPropagatesCancellation()
    {
        using var cancellation = new CancellationTokenSource(); var ct = cancellation.Token; var q = new PeriodQuery(day, day, farm);
        IReadOnlyCollection<Guid> ids = [farm]; farms.Setup(f => f.GetAccessibleFarmIdsAsync(ct)).ReturnsAsync(ids);
        reader.Setup(r => r.AnalyticsAsync(q, ids, ct)).ReturnsAsync(new AnalyticsInputs([], [], [], [], [], []));
        var result = await new AnalyticsService(reader.Object, farms.Object).GetAsync(q, ct);
        Assert.Null(result.Reproduction.PregnancyPercent); reader.Verify(r => r.AnalyticsAsync(q, ids, ct), Times.Once);
    }
    [Fact]
    public async Task InvalidAnalyticsPeriodNeverReadsInfrastructure()
    {
        await Assert.ThrowsAsync<ValidationException>(() => new AnalyticsService(reader.Object, farms.Object).GetAsync(new(day, day.AddDays(-1))));
        farms.Verify(f => f.GetAccessibleFarmIdsAsync(It.IsAny<CancellationToken>()), Times.Never); reader.VerifyNoOtherCalls();
    }
    [Theory] [InlineData(true)] [InlineData(false)]
    public async Task ReportPaginationPreservesClinicalFlagAndFarmScope(bool clinical)
    {
        IReadOnlyCollection<Guid> ids = [farm]; var q = new ReportQuery(day, day, farm, 2, 10);
        farms.Setup(f => f.GetAccessibleFarmIdsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(ids);
        var expected = new ReportResult(new([], 0, 2, 10), DateTime.UtcNow);
        reader.Setup(r => r.ReportAsync(q, clinical, ids, It.IsAny<CancellationToken>())).ReturnsAsync(expected);
        Assert.Same(expected, await new ReportService(reader.Object, farms.Object).GetAsync(q, clinical));
        reader.Verify(r => r.ReportAsync(q, clinical, ids, It.IsAny<CancellationToken>()), Times.Once);
    }
    [Fact]
    public async Task ExportRequestsTheBoundedExportPageRatherThanBrowserPagination()
    {
        IReadOnlyCollection<Guid> ids = [farm]; farms.Setup(f => f.GetAccessibleFarmIdsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(ids);
        reader.Setup(r => r.ReportAsync(new(day, day, farm, 1, 10000), false, ids, It.IsAny<CancellationToken>())).ReturnsAsync(new ReportResult(new([], 0, 1, 10000), DateTime.UtcNow));
        await new ReportService(reader.Object, farms.Object).ExportAsync(new(day, day, farm), false);
        reader.VerifyAll(); farms.VerifyAll();
    }
    [Fact]
    public async Task OversizedReportPageFailsBeforeDataAccess()
    {
        await Assert.ThrowsAsync<ValidationException>(() => new ReportService(reader.Object, farms.Object).GetAsync(new(day, day, farm, 1, 101), true));
        reader.VerifyNoOtherCalls(); farms.VerifyNoOtherCalls();
    }
}
