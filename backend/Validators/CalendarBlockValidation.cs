using FluentValidation;
using HostGrid.Api.DTOs.Booking;
using HostGrid.Api.Enums;

namespace HostGrid.Api.Validators;

internal static class CalendarBlockValidation
{
    internal static void Register(IServiceCollection services)
    {
        var createRules = new InlineValidator<CalendarBlockWrite>();
        Rules(createRules);
        SecurityValidationFilter.Add<CalendarBlockCreate>(services, rules =>
        {
            rules.RuleFor(row => row.PropertyId).NotEmpty();
            rules.RuleFor(row => row.Block).NotNull().SetValidator(createRules);
        });
        SecurityValidationFilter.Add<CalendarBlockWrite>(services, rules =>
        {
            Rules(rules);
            rules.RuleFor(row => row.Version).Must(version => version is { Length: 8 });
        });
        SecurityValidationFilter.Add<CalendarBlockCancel>(services, rules =>
        {
            rules.RuleFor(row => row.Version).Must(version => version is { Length: 8 });
            rules.RuleFor(row => row.Reason).NotEmpty().MaximumLength(500);
        });
    }

    private static void Rules(InlineValidator<CalendarBlockWrite> rules)
    {
        rules.RuleFor(row => row.StartDate).NotEmpty();
        rules.RuleFor(row => row.EndDate).GreaterThan(row => row.StartDate);
        rules.RuleFor(row => row.Reason).NotNull().IsInEnum();
        rules.RuleFor(row => row.Description).MaximumLength(500);
        rules.RuleFor(row => row.Description).Must(text => text?.Trim().Length >= 3)
            .When(row => row.Reason == CalendarBlockReason.Other);
    }
}
