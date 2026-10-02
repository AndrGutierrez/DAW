using Core.Application.Security;

namespace Core.Tests;

public sealed class AuthRequestValidationTests
{
    [Theory]
    [InlineData("weak")]
    [InlineData("password123!")]
    [InlineData("PASSWORD123!")]
    [InlineData("Passwordonly!")]
    [InlineData("Password123")]
    public async Task RegistrationRejectsPasswordsThatDoNotMeetIdentityPolicy(string password)
    {
        var result = await new RegisterRequestValidator().ValidateAsync(
            new RegisterRequest("operator", "operator@example.com", password, "Operator"));

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(RegisterRequest.Password));
    }

    [Fact]
    public async Task RegistrationReportsInvalidIdentityFieldsWithoutIncludingThePassword()
    {
        var result = await new RegisterRequestValidator().ValidateAsync(
            new RegisterRequest("a", "invalid", "secret", ""));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(RegisterRequest.Username));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(RegisterRequest.Email));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(RegisterRequest.FullName));
        Assert.All(result.Errors, error => Assert.DoesNotContain("secret", error.ErrorMessage));
    }

    [Fact]
    public async Task LoginAcceptsAnEmailIdentifierAndRefreshRequiresAToken()
    {
        Assert.True((await new LoginRequestValidator().ValidateAsync(
            new LoginRequest("operator@example.com", "User123!Test"))).IsValid);
        Assert.False((await new LoginRequestValidator().ValidateAsync(new LoginRequest(" ", ""))).IsValid);
        Assert.False((await new RefreshRequestValidator().ValidateAsync(new RefreshRequest(" "))).IsValid);
    }
}
