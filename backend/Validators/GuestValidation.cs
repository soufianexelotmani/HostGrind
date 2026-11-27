using FluentValidation;
using HostGrid.Api.DTOs.Booking;

namespace HostGrid.Api.Validators;

internal static class GuestValidation
{
    internal static void Register(IServiceCollection services)
    {
        SecurityValidationFilter.Add<GuestWrite>(services, rules =>
        {
            rules.RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            rules.RuleFor(x => x.Email).MaximumLength(254).Must(ValidEmail);
            rules.RuleFor(x => x.Phone).MaximumLength(32).Must(ValidPhone);
            rules.RuleFor(x => x.Version).Must(x => x is null || x.Length == 8);
        });
        SecurityValidationFilter.Add<GuestLookup>(services, rules =>
        {
            rules.RuleFor(x => x.Email).MaximumLength(254).Must(ValidEmail);
            rules.RuleFor(x => x.Phone).MaximumLength(32).Must(ValidPhone);
        });
    }

    private static bool ValidEmail(string? text) => string.IsNullOrWhiteSpace(text) ||
        System.Net.Mail.MailAddress.TryCreate(text.Trim(), out var address) && address.Address == text.Trim();

    private static bool ValidPhone(string? text) => string.IsNullOrWhiteSpace(text) ||
        text.Trim().Where((c, i) => i != 0 || c != '+')
            .All(c => char.IsDigit(c) || c is ' ' or '-' or '(' or ')' or '.' or '\u00a0') &&
        NormalizePhone(text) is { Length: >= 4 and <= 15 };

    internal static string? NormalizePhone(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var digits = string.Concat(text.Where(char.IsDigit).Select(c => (char)('0' + char.GetNumericValue(c))));
        if (digits.StartsWith("00", StringComparison.Ordinal)) return digits[2..];
        return digits.Length == 10 && digits[0] == '0' && !text.TrimStart().StartsWith('+') ? "212" + digits[1..] : digits;
    }
}
