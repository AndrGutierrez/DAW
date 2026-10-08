using System.Linq.Expressions;
using Core.Application.Management;
using Core.Application.Operations;
using Core.Application.Security;
using Core.Domain.Livestock;
using FluentValidation;
using Moq;
namespace UnitTests;
public sealed class ExchangeRateServiceTests
{
    private readonly Mock<IManagementRepository> repo = new(MockBehavior.Strict);
    private readonly Mock<ICurrentUser> user = new(MockBehavior.Strict);
    private readonly DateOnly day = DateOnly.FromDateTime(DateTime.UtcNow);
    private ExchangeRateService Service => new(repo.Object, user.Object, new ExchangeRateRequestValidator());
    public ExchangeRateServiceTests()
    {
        repo.Setup(r => r.ExecuteWriteAsync(It.IsAny<Func<Task<ExchangeRateQuote>>>(), It.IsAny<CancellationToken>())).Returns((Func<Task<ExchangeRateQuote>> action, CancellationToken _) => action());
    }
    [Fact]
    public async Task UnconfiguredCurrencyHasNoInventedRate()
    {
        repo.Setup(r => r.ListAsync(It.IsAny<Expression<Func<ExchangeRate, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        Assert.Null(await Service.GetAsync(day)); repo.Verify(r => r.SaveAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
    [Fact]
    public async Task FutureRateIsNotAppliedAndLatestCorrectionIsChosenByDate()
    {
        var older = EntityTestTime.At(new ExchangeRate { EffectiveDate = day.AddDays(-1), BolivarsPerDollar = 40 }, DateTime.UtcNow.AddHours(-2));
        var corrected = EntityTestTime.At(new ExchangeRate { EffectiveDate = day.AddDays(-1), BolivarsPerDollar = 41.123456m }, DateTime.UtcNow.AddHours(-1));
        var future = new ExchangeRate { EffectiveDate = day.AddDays(1), BolivarsPerDollar = 50 };
        repo.Setup(r => r.ListAsync(It.IsAny<Expression<Func<ExchangeRate, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync((Expression<Func<ExchangeRate, bool>> filter, CancellationToken _) => new[] { future, older, corrected }.Where(filter.Compile()).ToArray());
        var result = await Service.GetAsync(day); Assert.Equal(corrected.Id, result!.Id); Assert.Equal(41.123456m, result.BolivarsPerDollar); Assert.Equal("manual", result.EntryMethod);
    }
    [Theory] [InlineData(0)] [InlineData(-1)] [InlineData(1000000000)]
    public async Task InvalidRateCannotReadOrPersistAnEntry(decimal amount)
    {
        await Assert.ThrowsAsync<ValidationException>(() => Service.RecordAsync(new(Guid.NewGuid(), day, amount)));
        repo.Verify(r => r.GetAsync<ExchangeRate>(It.IsAny<Guid>(), false, It.IsAny<CancellationToken>()), Times.Never);
        repo.Verify(r => r.SaveAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
    [Fact]
    public async Task EntryRetainsSixDecimalsAndAuthorAndReplayNeverWritesAgain()
    {
        var id = Guid.NewGuid(); var author = Guid.NewGuid(); ExchangeRate? entry = null;
        user.SetupGet(u => u.UserId).Returns(author);
        repo.Setup(r => r.GetAsync<ExchangeRate>(id, false, It.IsAny<CancellationToken>())).ReturnsAsync(() => entry);
        repo.Setup(r => r.Add(It.IsAny<ExchangeRate>())).Callback<ExchangeRate>(r => entry = r);
        repo.Setup(r => r.SaveAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var request = new ExchangeRateRequest(id, day, 41.123456m); await Service.RecordAsync(request); await Service.RecordAsync(request);
        Assert.Equal(author, entry!.RecordedByUserId); Assert.Equal(41.123456m, entry.BolivarsPerDollar);
        await Assert.ThrowsAsync<ConflictException>(() => Service.RecordAsync(request with { BolivarsPerDollar = 42 }));
        repo.Verify(r => r.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
