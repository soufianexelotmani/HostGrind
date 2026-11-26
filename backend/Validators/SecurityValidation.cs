using FluentValidation;
using HostGrid.Api.DTOs.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace HostGrid.Api.Validators;

internal sealed class SecurityValidationFilter(IServiceProvider services) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var argument in context.ActionArguments.Values.OfType<object>())
        {
            if (services.GetService(typeof(IValidator<>).MakeGenericType(argument.GetType())) is not IValidator validator)
                continue;

            var validation = await validator.ValidateAsync(new ValidationContext<object>(argument), context.HttpContext.RequestAborted);
            foreach (var failure in validation.Errors)
                context.ModelState.AddModelError(failure.PropertyName, failure.ErrorMessage);
        }

        if (!context.ModelState.IsValid)
            context.Result = new BadRequestObjectResult(new ValidationProblemDetails(context.ModelState));
        else
            await next();
    }

    internal static void Register(IServiceCollection services)
    {
        Add<LoginRequest>(services, rules => {
            Email(rules.RuleFor(row => row.Email));
            rules.RuleFor(row => row.Password).NotEmpty().MaximumLength(256);
        });
        Add<EmailRequest>(services, rules => Email(rules.RuleFor(row => row.Email)));
        Add<PasswordLinkRequest>(services, rules => {
            Email(rules.RuleFor(row => row.Email));
            rules.RuleFor(row => row.Token).NotEmpty().MaximumLength(4096);
            rules.RuleFor(row => row.Password).NotEmpty().Length(12, 256);
        });
        Add<PasswordChangeRequest>(services, rules => {
            rules.RuleFor(row => row.CurrentPassword).NotEmpty().MaximumLength(256);
            rules.RuleFor(row => row.Password).NotEmpty().Length(12, 256);
        });
        Add<MfaChallengeRequest>(services, rules => rules.RuleFor(row => row.Challenge).NotEmpty().Length(43));
        Add<MfaStartRequest>(services, rules => rules.RuleFor(row => row.Password).NotEmpty().MaximumLength(256));
        Add<MfaVerifyRequest>(services, rules => {
            rules.RuleFor(row => row.Challenge).NotEmpty().Length(43);
            rules.RuleFor(row => row.Code).NotEmpty().MaximumLength(40);
        });
        Add<MfaResetRequest>(services, rules => {
            rules.RuleFor(row => row.Password).NotEmpty().MaximumLength(256);
            rules.RuleFor(row => row.Code).NotEmpty().MaximumLength(40);
        });
        Add<InviteRequest>(services, rules => {
            Email(rules.RuleFor(row => row.Email));
            rules.RuleFor(row => row.Name).NotEmpty().MaximumLength(200);
            rules.RuleFor(row => row.Role).NotEmpty().MaximumLength(32);
        });
        Add<MembershipChangeRequest>(services, rules => rules.RuleFor(row => row.Role).NotEmpty().MaximumLength(32));
        Add<OrganizationRequest>(services, rules => {
            rules.RuleFor(row => row.Name).NotEmpty().MaximumLength(200);
            rules.RuleFor(row => row.Reference).NotEmpty().MaximumLength(40);
        });
    }

    private static void Email<T>(IRuleBuilderInitial<T, string> rule)
        => rule.NotEmpty().MaximumLength(254).EmailAddress();

    internal static void Add<T>(IServiceCollection services, Action<InlineValidator<T>> configure)
    {
        var validator = new InlineValidator<T>();
        configure(validator);
        services.AddSingleton<IValidator<T>>(validator);
    }
}
