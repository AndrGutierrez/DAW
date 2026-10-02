using FluentValidation;

namespace Core.Application.Security;

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(request => request.Username).Cascade(CascadeMode.Stop)
            .NotEmpty().Length(3, 100).Matches("^[a-zA-Z0-9._@+-]+$");
        RuleFor(request => request.Email).Cascade(CascadeMode.Stop)
            .NotEmpty().MaximumLength(256).EmailAddress();
        RuleFor(request => request.Password).Cascade(CascadeMode.Stop)
            .NotEmpty().Length(8, 256).Matches("[a-z]").Matches("[A-Z]")
            .Matches("[0-9]").Matches("[^a-zA-Z0-9]");
        RuleFor(request => request.FullName).NotEmpty().MaximumLength(200);
    }
}

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(request => request.Username).NotEmpty().MaximumLength(256);
        RuleFor(request => request.Password).NotEmpty().MaximumLength(256);
    }
}

public sealed class RefreshRequestValidator : AbstractValidator<RefreshRequest>
{
    public RefreshRequestValidator()
    {
        RuleFor(request => request.RefreshToken).NotEmpty().MaximumLength(256);
    }
}
